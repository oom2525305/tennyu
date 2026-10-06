using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

// Reflection keeps the existing Assembly-CSharp / Inspector references intact.
// Run in Unity Test Runner's PlayMode tab. These tests have not been run in the cloud.
public sealed class GameplayRegressionTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private SimulationMode2D previousSimulation;
    private float previousScale, previousStep;
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object instance, string method, params object[] args) =>
        instance.GetType().GetMethod(method, Members).Invoke(instance, args);
    private static void Set(object instance, string field, object value) =>
        instance.GetType().GetField(field, Members).SetValue(instance, value);
    private static object Get(object instance, string field) =>
        instance.GetType().GetField(field, Members).GetValue(instance);
    private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
    private GameObject Make(string name, bool active = true)
    {
        var go = new GameObject(name);
        go.SetActive(active);
        objects.Add(go);
        return go;
    }
    private Component Director()
    {
        return Make("Director").AddComponent(TypeOf("Tenyu.Gameplay.StageDirector"));
    }
    private Rigidbody2D Body()
    {
        var go = Make("Body");
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        return body;
    }
    private void Wall(Vector2 center, Vector2 size)
    {
        var go = Make("Wall"); go.layer = 7; go.transform.position = center;
        go.AddComponent<BoxCollider2D>().size = size;
    }
    private Vector2 Resolve(Rigidbody2D body, Vector2 delta)
    {
        Physics2D.SyncTransforms();
        return (Vector2)TypeOf("Tenyu.Gameplay.KinematicMotion2D").GetMethod("Resolve").Invoke(null,
            new object[] { body, delta, (LayerMask)(1 << 7), 0.02f, new List<RaycastHit2D>() });
    }
    [SetUp] public void Setup()
    {
        previousSimulation = Physics2D.simulationMode;
        previousScale = Time.timeScale; previousStep = Time.fixedDeltaTime;
        Physics2D.simulationMode = SimulationMode2D.Script;
        Time.fixedDeltaTime = 0.02f;
        TypeOf("Tenyu.Gameplay.StageTime").GetMethod("Reset").Invoke(null, null);
    }
    [TearDown] public void Cleanup()
    {
        foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
        objects.Clear();
        TypeOf("Tenyu.Gameplay.StageTime").GetMethod("Reset").Invoke(null, null);
        Physics2D.simulationMode = previousSimulation;
        Time.timeScale = previousScale; Time.fixedDeltaTime = previousStep;
    }
    [Test] public void HardSelectionUsesItsPrefabAndKeepsLegacyClearRecordsSeparate()
    {
        var session = TypeOf("Tenyu.SceneFlow.GameSession");
        var progress = TypeOf("Tenyu.Timeline.StageProgress");
        var mode = TypeOf("Tenyu.SceneFlow.StageDifficulty");
        var normal = Enum.Parse(mode, "Normal"); var hard = Enum.Parse(mode, "Hard");
        var previousMode = session.GetProperty("SelectedDifficulty").GetValue(null);
        const int stage = 928371;
        string normalKey = "StageCleared_" + stage, hardKey = normalKey + "_Hard";
        bool hadNormal = PlayerPrefs.HasKey(normalKey), hadHard = PlayerPrefs.HasKey(hardKey);
        int oldNormal = PlayerPrefs.GetInt(normalKey), oldHard = PlayerPrefs.GetInt(hardKey);
        try
        {
            PlayerPrefs.DeleteKey(normalKey); PlayerPrefs.DeleteKey(hardKey);
            progress.GetMethod("SetCleared").Invoke(null, new object[] { stage, hard });
            Assert.That(progress.GetMethod("IsCleared").Invoke(null, new object[] { stage, normal }), Is.False);
            Assert.That(progress.GetMethod("IsCleared").Invoke(null, new object[] { stage, hard }), Is.True);
            Assert.That(progress.GetMethod("IsUnlocked").Invoke(null, new object[] { stage + 1 }), Is.True);
            // Previously saved normal records still use the original key.
            PlayerPrefs.SetInt(normalKey, 1);
            Assert.That(progress.GetMethod("IsCleared").Invoke(null, new object[] { stage, normal }), Is.True);

            var loader = Make("Loader", false).AddComponent(TypeOf("Tenyu.Gameplay.StageLoader"));
            var entryType = loader.GetType().GetNestedType("StageEntry", BindingFlags.NonPublic);
            var entry = Activator.CreateInstance(entryType, true);
            var normalPrefab = Make("Normal", false); var hardPrefab = Make("Hard", false);
            Set(entry, "stageId", stage); Set(entry, "stagePrefab", normalPrefab); Set(entry, "hardStagePrefab", hardPrefab);
            var entries = Array.CreateInstance(entryType, 1); entries.SetValue(entry, 0); Set(loader, "stages", entries);
            session.GetMethod("SelectDifficulty").Invoke(null, new[] { hard });
            Assert.That(Call(loader, "FindStagePrefab", stage), Is.SameAs(hardPrefab));
            session.GetMethod("SelectDifficulty").Invoke(null, new[] { normal });
            Assert.That(Call(loader, "FindStagePrefab", stage), Is.SameAs(normalPrefab));
        }
        finally
        {
            session.GetMethod("SelectDifficulty").Invoke(null, new[] { previousMode });
            if (hadNormal) PlayerPrefs.SetInt(normalKey, oldNormal); else PlayerPrefs.DeleteKey(normalKey);
            if (hadHard) PlayerPrefs.SetInt(hardKey, oldHard); else PlayerPrefs.DeleteKey(hardKey);
            PlayerPrefs.Save();
        }
    }

    [Test] public void DiagonalWindKeepsItsActualDirection()
    {
        var go = Make("Ability");
        var button = go.AddComponent(TypeOf("AbilityDragButton"));
        Set(button, "rootCanvas", Make("Canvas").AddComponent<Canvas>());
        Set(button, "lockedScreenPosition", Vector2.zero);
        Call(button, "UpdateSelectedDirection", new Vector2(80f, 40f));
        Assert.That(Vector2.Distance((Vector2)Get(button, "selectedScreenDirection"), new Vector2(2f, 1f).normalized), Is.LessThan(0.001f));
    }
    [Test] public void WallContactSlidesInsteadOfStoppingBothAxes()
    {
        var body = Body(); Wall(new Vector2(1f, 0f), new Vector2(0.2f, 10f));
        Vector2 position = Resolve(body, new Vector2(2f, 1f));
        Assert.That(position.x, Is.InRange(0.45f, 0.5f));
        Assert.That(position.y, Is.GreaterThan(0.95f));
    }
    [Test] public void SlidingCannotCutThroughAnInsideCorner()
    {
        var body = Body(); Wall(new Vector2(1f, 0f), new Vector2(0.2f, 10f));
        Wall(new Vector2(0f, 1.5f), new Vector2(10f, 0.2f));
        Vector2 position = Resolve(body, new Vector2(2f, 2f));
        Assert.That(position.x, Is.LessThanOrEqualTo(0.5f));
        Assert.That(position.y, Is.InRange(0.95f, 1.0f));
    }
    [Test] public void SmoothedPathStillClearsWallsWithTheWholeBody()
    {
        var body = Body(); Wall(new Vector2(2f, 0f), new Vector2(0.3f, 2f));
        var finder = Make("Pathfinder").AddComponent(TypeOf("Tenyu.Gameplay.GridPathfinder2D"));
        var path = new List<Vector2>();
        Physics2D.SyncTransforms();
        Assert.That((bool)Call(finder, "TryFindPath", body, new Vector2(4f, 0f),
            (LayerMask)(1 << 7), 0.02f, path), Is.True);
        Assert.That(path.Count, Is.InRange(2, 8));
        Vector2 from = body.position;
        foreach (Vector2 to in path)
        {
            Assert.That((bool)Call(finder, "CanTravel", body, from, to, (LayerMask)(1 << 7), 0.02f), Is.True);
            from = to;
        }
    }
    [Test] public void AutomaticWalkingDoesNotFightWindAndRetryClearsVelocity()
    {
        var director = Director();
        var go = Make("Walker", false);
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        var walker = go.AddComponent(TypeOf("Tenyu.Gameplay.RouteWalker2D"));
        var start = Make("Start").transform;
        var goal = Make("Goal").transform; goal.position = Vector3.left * 4f;
        Set(walker, "stageDirector", director);
        Set(walker, "waypoints", new[] { start, goal });
        Set(walker, "pathfinder", Make("Pathfinder").AddComponent(TypeOf("Tenyu.Gameplay.GridPathfinder2D")));
        go.SetActive(true); Call(walker, "ResetWalker"); Call(director, "BeginStage");
        Call(walker, "ApplyWind", Vector2.right, 6f);
        Call(walker, "MoveCharacter"); Physics2D.Simulate(0.02f);
        Assert.That(body.position.x, Is.EqualTo(0.12f).Within(0.005f));
        Call(director, "ResetStage");
        Assert.That(body.position, Is.EqualTo(Vector2.zero));
        Assert.That((Vector2)Get(walker, "windVelocity"), Is.EqualTo(Vector2.zero));
    }
    [Test] public void NearCornerDoesNotSkipIntoAnObstructedNextSegment()
    {
        var go = Make("Walker", false);
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.position = new Vector2(-0.01f, 0f);
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        var walker = go.AddComponent(TypeOf("Tenyu.Gameplay.RouteWalker2D"));
        var finder = Make("Pathfinder").AddComponent(TypeOf("Tenyu.Gameplay.GridPathfinder2D"));
        Set(walker, "pathfinder", finder);
        var goal = Make("Goal").transform; goal.position = new Vector3(0.01f, 2f, 0f);
        go.SetActive(true);
        Wall(new Vector2(-0.9f, 1f), new Vector2(1f, 1f));
        Physics2D.SyncTransforms();
        Vector2 corner = new Vector2(0.01f, 0f);
        Assert.That((bool)Call(finder, "CanTravel", body, body.position, corner, (LayerMask)128, 0.02f), Is.True);
        Assert.That((bool)Call(finder, "CanTravel", body, body.position, (Vector2)goal.position, (LayerMask)128, 0.02f), Is.False);
        Set(walker, "obstacleLayers", (LayerMask)128);
        Set(walker, "pathRequested", true); Set(walker, "pathTarget", goal);
        Set(walker, "lastPathGoal", (Vector2)goal.position); Set(walker, "repathTimer", 1f);
        ((List<Vector2>)Get(walker, "plannedPath")).AddRange(new[] { corner, (Vector2)goal.position });
        Vector2 next = (Vector2)Call(walker, "FollowPath", goal, 0f, 0.02f);
        Assert.That(next.x, Is.EqualTo(corner.x).Within(0.001f));
        Assert.That(next.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That((int)Get(walker, "pathIndex"), Is.EqualTo(0));
    }
    [Test] public void GoalDoesNotClearDuringObservation()
    {
        var director = Director();
        var targetGo = Make("Target", false);
        var target = targetGo.AddComponent(TypeOf("Tenyu.Gameplay.ProtectedTarget2D"));
        Set(target, "stageDirector", director); var collider = targetGo.AddComponent<CircleCollider2D>();
        targetGo.SetActive(true);
        var goalGo = Make("Goal"); goalGo.AddComponent<BoxCollider2D>();
        var goal = goalGo.AddComponent(TypeOf("StageGoal2D"));
        Set(goal, "stageDirector", director);
        Call(goal, "TryClear", collider);
        Assert.That(Property(director, "Phase").ToString(), Is.EqualTo("Observation"));
        Call(director, "BeginStage"); Call(goal, "TryClear", collider);
        Assert.That(Property(director, "Phase").ToString(), Is.EqualTo("Cleared"));
    }
    [Test] public void RockIgnoresWindBeforeAndDuringFallAndStillResets()
    {
        var director = Director(); var root = Make("Rock", false);
        var rock = root.AddComponent(TypeOf("Tenyu.Gameplay.FallingRock2D"));
        var impact = Make("Impact").transform; impact.SetParent(root.transform);
        var receiverGo = Make("Receiver"); receiverGo.transform.SetParent(root.transform);
        var receiver = receiverGo.AddComponent(TypeOf("DivinePowerReceiver2D"));
        Set(rock, "stageDirector", director); Set(rock, "impactPoint", impact); Set(rock, "powerReceiver", receiver);
        root.SetActive(true); Call(rock, "ResetRock");
        Call(receiver, "ReceiveThunder", 1f);
        Assert.That(Get(rock, "state").ToString(), Is.EqualTo("Waiting"));
        Call(director, "BeginStage");
        Call(receiver, "ReceiveWind", Vector2.right, 6f);
        Assert.That((Vector3)Get(rock, "targetPosition"), Is.EqualTo(Vector3.zero));
        Assert.That(receiverGo.transform.position, Is.EqualTo(Vector3.zero));
        Call(receiver, "ReceiveThunder", 1f);
        Assert.That(Get(rock, "state").ToString(), Is.EqualTo("Falling"));
        Call(receiver, "ReceiveWind", Vector2.right, 6f);
        Assert.That(((Vector3)Get(rock, "targetPosition")).x, Is.EqualTo(0f).Within(0.001f));
        Call(director, "ResetStage");
        Assert.That((Vector3)Get(rock, "targetPosition"), Is.EqualTo(Vector3.zero));
        Assert.That(Get(rock, "state").ToString(), Is.EqualTo("Waiting"));
    }
    [TestCase(false)]
    [TestCase(true)]
    public void AmbushWaitsUntilApproachOrGhostAndResets(bool useGhost)
    {
        var director = Director();
        var targetGo = Make("Nobunaga", false);
        targetGo.transform.position = Vector3.right * 5f;
        var target = targetGo.AddComponent(TypeOf("Tenyu.Gameplay.ProtectedTarget2D"));
        Set(target, "stageDirector", director); targetGo.SetActive(true);
        var go = Make("Ambush", false);
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        go.AddComponent(TypeOf("DivinePowerReceiver2D"));
        var chaser = go.AddComponent(TypeOf("Tenyu.Gameplay.AssassinChaser2D"));
        Set(chaser, "stageDirector", director); Set(chaser, "target", target);
        Set(chaser, "startDelay", 0f); Set(chaser, "ambushDistance", 3f);
        Set(chaser, "obstacleLayers", (LayerMask)(1 << 7));
        Set(chaser, "pathfinder", Make("Grid").AddComponent(TypeOf("Tenyu.Gameplay.GridPathfinder2D")));
        go.SetActive(true); Call(chaser, "ResetChaser"); Call(director, "BeginStage");
        Physics2D.SyncTransforms(); Call(chaser, "FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime);
        Assert.That((bool)Get(chaser, "alerted"), Is.False);
        Assert.That(body.position, Is.EqualTo(Vector2.zero));
        GameObject ghostGo = null;
        if (useGhost)
        {
            ghostGo = Make("Ghost"); ghostGo.transform.position = Vector3.left;
            var ghost = ghostGo.AddComponent(TypeOf("Tenyu.Gameplay.GhostLure2D"));
            Call(ghost, "Initialize", director, 6f, 3f);
        }
        else targetGo.transform.position = Vector3.right * 2f;
        Physics2D.SyncTransforms();
        Call(director, "PauseStage"); Call(chaser, "FixedUpdate");
        Assert.That((bool)Get(chaser, "alerted"), Is.False, "Pause must not trigger an ambush");
        Call(director, "ResumeStage"); Call(chaser, "FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime);
        Assert.That((bool)Get(chaser, "alerted"), Is.True);
        Assert.That(useGhost ? body.position.x < 0f : body.position.x > 0f, Is.True);
        if (ghostGo != null) ghostGo.SetActive(false);
        targetGo.transform.position = Vector3.right * 5f;
        Physics2D.SyncTransforms(); Call(chaser, "FixedUpdate");
        Assert.That((bool)Get(chaser, "alerted"), Is.True, "A lured guard stays active");
        Call(chaser, "ResetChaser");
        Assert.That((bool)Get(chaser, "alerted"), Is.False);
        Assert.That(body.position, Is.EqualTo(Vector2.zero));
    }

    [Test] public void GhostCancelsAssassinAttackAndPullsItAwayFromNobunaga()
    {
        var director = Director();
        var targetGo = Make("Nobunaga", false); targetGo.layer = 6;
        targetGo.transform.position = Vector3.right * 0.5f;
        var target = targetGo.AddComponent(TypeOf("Tenyu.Gameplay.ProtectedTarget2D"));
        Set(target, "stageDirector", director); targetGo.AddComponent<CircleCollider2D>().radius = 0.2f;
        targetGo.SetActive(true);
        var go = Make("Assassin", false); go.layer = 6;
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        go.AddComponent(TypeOf("DivinePowerReceiver2D"));
        var chaser = go.AddComponent(TypeOf("Tenyu.Gameplay.AssassinChaser2D"));
        Set(chaser, "stageDirector", director); Set(chaser, "target", target);
        Set(chaser, "startDelay", 0f); Set(chaser, "obstacleLayers", (LayerMask)(1 << 7));
        Set(chaser, "pathfinder", Make("Grid").AddComponent(TypeOf("Tenyu.Gameplay.GridPathfinder2D")));
        go.SetActive(true); Call(chaser, "ResetChaser");
        var ghostGo = Make("Ghost"); ghostGo.transform.position = Vector3.left;
        var ghost = ghostGo.AddComponent(TypeOf("Tenyu.Gameplay.GhostLure2D"));
        Call(ghost, "Initialize", director, 6f, 3f);
        Call(director, "BeginStage"); Set(chaser, "windingUp", true); Set(chaser, "warning", 0f);
        Physics2D.SyncTransforms(); Call(chaser, "FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime);
        Assert.That(body.position.x, Is.LessThan(0f));
        Assert.That((bool)Get(chaser, "windingUp"), Is.False);
        Assert.That(Property(director, "Phase").ToString(), Is.EqualTo("Playing"));
        Call(director, "PauseStage"); float remaining = (float)Get(ghost, "remainingTime");
        Call(ghost, "Update"); Assert.That((float)Get(ghost, "remainingTime"), Is.EqualTo(remaining));
        ghostGo.SetActive(false);
        Assert.That(ghost.GetType().GetMethod("FindFor").Invoke(null, new object[] { director, body.position }), Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnreachableGhostDoesNotStopWalkerOrAssassin(bool separatedByWall)
    {
        var director = Director();
        var finder = Make("Grid").AddComponent(TypeOf("Tenyu.Gameplay.GridPathfinder2D"));
        Set(finder, "areaSize", new Vector2(6f, 6f));
        if (separatedByWall) Wall(Vector2.zero, new Vector2(0.3f, 8f));
        else Wall(Vector2.right, Vector2.one);
        var go = Make("Nobunaga", false); go.layer = 6;
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.position = Vector2.left;
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        var target = go.AddComponent(TypeOf("Tenyu.Gameplay.ProtectedTarget2D")); Set(target, "stageDirector", director);
        var walker = go.AddComponent(TypeOf("Tenyu.Gameplay.RouteWalker2D"));
        var start = Make("Start").transform; start.position = Vector3.left;
        var goal = Make("Goal").transform; goal.position = new Vector3(-1f, 2f, 0f);
        Set(walker, "stageDirector", director); Set(walker, "pathfinder", finder);
        Set(walker, "waypoints", new[] { start, goal });
        go.SetActive(true); Call(walker, "ResetWalker");
        var ninjaGo = Make("Ninja", false); ninjaGo.layer = 6;
        var ninjaBody = ninjaGo.AddComponent<Rigidbody2D>(); ninjaBody.bodyType = RigidbodyType2D.Kinematic;
        ninjaBody.position = new Vector2(-1f, -1f); ninjaGo.AddComponent<CircleCollider2D>().radius = 0.32f;
        ninjaGo.AddComponent(TypeOf("DivinePowerReceiver2D"));
        var ninja = ninjaGo.AddComponent(TypeOf("Tenyu.Gameplay.AssassinChaser2D"));
        Set(ninja, "stageDirector", director); Set(ninja, "target", target); Set(ninja, "pathfinder", finder);
        Set(ninja, "obstacleLayers", (LayerMask)128); Set(ninja, "startDelay", 0f);
        ninjaGo.SetActive(true); Call(ninja, "ResetChaser");
        var ghostGo = Make("UnreachableGhost"); ghostGo.transform.position = Vector3.right;
        var ghost = ghostGo.AddComponent(TypeOf("Tenyu.Gameplay.GhostLure2D"));
        Call(ghost, "Initialize", director, 6f, 3f); Call(director, "BeginStage");
        Physics2D.SyncTransforms();
        Assert.That(ghost.GetType().GetMethod("FindReachableFor").Invoke(null,
            new object[] { director, body, finder, (LayerMask)128, 0.02f }), Is.Null);
        Call(walker, "MoveCharacter"); Call(ninja, "FixedUpdate"); Physics2D.Simulate(0.02f);
        Assert.That(body.position.y, Is.GreaterThan(0f));
        Assert.That(ninjaBody.position.y, Is.GreaterThan(-1f));
    }

    [Test] public void LowerRoadAcceptsPlacementButVisibleAbilityUiDoesNot()
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); objects.Add(canvasGo);
        var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var areaGo = new GameObject("WorldCastArea", typeof(RectTransform)); objects.Add(areaGo);
        var area = areaGo.GetComponent<RectTransform>(); area.SetParent(canvas.transform, false);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.sizeDelta = Vector2.zero;
        var buttonGo = new GameObject("Ability", typeof(RectTransform)); objects.Add(buttonGo);
        var rect = buttonGo.GetComponent<RectTransform>(); rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(70f, 100f); rect.sizeDelta = new Vector2(130f, 130f);
        var button = buttonGo.AddComponent(TypeOf("AbilityDragButton"));
        Set(button, "worldCastArea", area); Call(button, "CacheBlockingUi"); Canvas.ForceUpdateCanvases();
        Assert.That(Call(button, "IsInsideWorldArea", new Vector2(Screen.width - 40f, 60f), null), Is.True);
        Assert.That(Call(button, "IsInsideWorldArea", new Vector2(70f, 100f), null), Is.False);
    }

    [Test] public void InvalidGhostDropKeepsPreviousGhostUsesAndCooldown()
    {
        var director = Director();
        var prefab = Make("GhostPrefab").AddComponent(TypeOf("Tenyu.Gameplay.GhostLure2D"));
        var go = Make("Powers", false); var powers = go.AddComponent(TypeOf("DivinePowerSystem2D"));
        Set(powers, "stageDirector", director); Set(powers, "ghostPrefab", prefab);
        go.SetActive(true); Call(director, "BeginStage");
        var type = Enum.Parse(TypeOf("DivinePowerType"), "Ghost");
        Assert.That(Call(powers, "Cast", type, Vector2.left, Vector2.zero), Is.True);
        var previous = (Component)Get(powers, "activeGhost"); objects.Add(previous.gameObject);
        int uses = (int)Call(powers, "GetRemainingUses", type);
        Set(powers, "remainingGhostCooldown", 0f);
        Wall(Vector2.right, Vector2.one); Physics2D.SyncTransforms();
        Assert.That(Call(powers, "Cast", type, Vector2.right, Vector2.zero), Is.False);
        Assert.That(Get(powers, "activeGhost"), Is.SameAs(previous));
        Assert.That((int)Call(powers, "GetRemainingUses", type), Is.EqualTo(uses));
        Assert.That((float)Call(powers, "GetRemainingCooldown", type), Is.EqualTo(0f));
    }

    [Test] public void ApproachRockWaitsForTargetAndCanBePreemptedWithThunder()
    {
        var director = Director(); var root = Make("ApproachRock", false);
        var rock = root.AddComponent(TypeOf("Tenyu.Gameplay.FallingRock2D"));
        var impact = Make("Impact").transform; impact.SetParent(root.transform);
        var receiverGo = Make("Receiver"); receiverGo.transform.SetParent(root.transform);
        var receiver = receiverGo.AddComponent(TypeOf("DivinePowerReceiver2D"));
        Set(rock, "stageDirector", director); Set(rock, "impactPoint", impact); Set(rock, "powerReceiver", receiver);
        Set(rock, "triggerOnApproach", true); Set(rock, "warningTime", 0.8f);
        root.SetActive(true); Call(rock, "ResetRock"); Call(director, "BeginStage");
        Call(rock, "UpdateWarning");
        Assert.That((bool)Get(rock, "warningStarted"), Is.False);
        Assert.That((float)Get(rock, "remainingWarningTime"), Is.EqualTo(0.8f));
        var target = Make("Nobunaga"); target.transform.position = Vector3.right * 1.5f;
        target.AddComponent(TypeOf("Tenyu.Gameplay.ProtectedTarget2D")); target.AddComponent<CircleCollider2D>();
        Physics2D.SyncTransforms(); Call(rock, "UpdateWarning");
        Assert.That((bool)Get(rock, "warningStarted"), Is.True);
        Call(director, "ResetStage");
        Assert.That((bool)Get(rock, "warningStarted"), Is.False);
        Call(director, "BeginStage"); Call(receiver, "ReceiveThunder", 1f);
        Assert.That(Get(rock, "state").ToString(), Is.EqualTo("Falling"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BridgeClockPausesAndOnlyHitsTargetsStillOnBridge(bool escape)
    {
        var director = Director();
        var bridgeGo = Make("Bridge", false);
        var bridge = bridgeGo.AddComponent(TypeOf("Tenyu.Gameplay.CollapsingBridge2D"));
        Set(bridge, "stageDirector", director); Set(bridge, "collapseDelay", 0.1f);
        bridgeGo.SetActive(true); Call(bridge, "ResetBridge");
        var targetGo = Make("Nobunaga", false); targetGo.layer = 6;
        targetGo.AddComponent<CircleCollider2D>().radius = 0.4f;
        var target = targetGo.AddComponent(TypeOf("Tenyu.Gameplay.ProtectedTarget2D"));
        Set(target, "stageDirector", director); targetGo.SetActive(true);
        Physics2D.SyncTransforms(); Call(bridge, "FixedUpdate");
        Assert.That((bool)Get(bridge, "triggered"), Is.False, "Observation must not arm a bridge");
        Call(director, "BeginStage"); Call(bridge, "FixedUpdate");
        Assert.That((bool)Get(bridge, "triggered"), Is.True);
        Call(director, "PauseStage"); float remaining = (float)Get(bridge, "remaining");
        Call(bridge, "FixedUpdate");
        Assert.That((float)Get(bridge, "remaining"), Is.EqualTo(remaining));
        if (escape) targetGo.transform.position = Vector3.right * 4f;
        Physics2D.SyncTransforms(); Call(director, "ResumeStage");
        for (int i = 0; i < 10; i++) Call(bridge, "FixedUpdate");
        Assert.That((bool)Get(bridge, "collapsed"), Is.True);
        Assert.That(Property(director, "Phase").ToString(), Is.EqualTo(escape ? "Playing" : "Failed"));
        Call(director, "ResetStage");
        Assert.That((bool)Get(bridge, "collapsed"), Is.False);
        Assert.That((bool)Get(bridge, "triggered"), Is.False);
        Assert.That((float)Get(bridge, "remaining"), Is.EqualTo(0.1f));
    }

    [Test] public void SoundSettingsClampAndUpdateExistingSourcesWithoutCompounding()
    {
        var settings = TypeOf("Tenyu.Audio.TenyuAudioSettings");
        const string key = "Tenyu.Audio.Effects";
        bool existed = PlayerPrefs.HasKey(key); float old = PlayerPrefs.GetFloat(key, 1);
        var soundGo = Make("Guarded sound", false);
        var source = soundGo.AddComponent<AudioSource>(); source.volume = 0.6f;
        soundGo.AddComponent(TypeOf("Tenyu.Audio.TenyuStageSourceGuard"));
        soundGo.SetActive(true);
        try
        {
            settings.GetMethod("SetEffects").Invoke(null, new object[] { 0.5f });
            Assert.That(source.volume, Is.EqualTo(0.3f).Within(0.001f));
            settings.GetMethod("SetEffects").Invoke(null, new object[] { 0.5f });
            Assert.That(source.volume, Is.EqualTo(0.3f).Within(0.001f));
            settings.GetMethod("SetEffects").Invoke(null, new object[] { -1f });
            Assert.That(source.volume, Is.Zero);
            settings.GetMethod("SetEffects").Invoke(null, new object[] { 2f });
            Assert.That(source.volume, Is.EqualTo(0.6f).Within(0.001f));
            settings.GetMethod("Save").Invoke(null, null);
            Assert.That(PlayerPrefs.GetFloat(key), Is.EqualTo(1f));
        }
        finally
        {
            settings.GetMethod("SetEffects").Invoke(null, new object[] { old });
            if (!existed) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    [Test] public void RainExtinguishesAndWetFenceCannotIgniteUntilDry()
    {
        var director = Director(); var go = Make("Fence", false);
        var receiver = go.AddComponent(TypeOf("DivinePowerReceiver2D"));
        var flame = go.AddComponent(TypeOf("Tenyu.Gameplay.FlammableObject2D"));
        var wet = go.AddComponent(TypeOf("Tenyu.Gameplay.WettableObject2D"));
        Set(flame, "stageDirector", director); Set(flame, "powerReceiver", receiver);
        Set(flame, "preventIgnitionWhileWet", true);
        Set(wet, "stageDirector", director); Set(wet, "powerReceiver", receiver);
        go.SetActive(true); Call(flame, "ResetState"); Call(wet, "ResetState");
        Call(receiver, "ReceiveRain", 1f); Call(receiver, "ReceiveThunder", 1f);
        Assert.That(Property(flame, "IsBurning"), Is.False);
        Call(wet, "Dry"); Call(receiver, "ReceiveThunder", 1f);
        Assert.That(Property(flame, "IsBurning"), Is.True);
        Call(receiver, "ReceiveRain", 1f);
        Assert.That(Property(flame, "IsBurning"), Is.False);
    }
}
