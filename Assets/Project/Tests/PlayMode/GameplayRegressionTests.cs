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
