using UnityEngine;
using TMPro;

namespace Tenyu.Gameplay
{
    public sealed class BurningTownRoute2D : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private RouteWalker2D walker;
        [SerializeField] private FlammableObject2D fence;
        [SerializeField] private Transform[] mainRoute;
        [SerializeField] private Transform[] backRoute;
        [SerializeField] private TMP_Text hint;
        private bool usingBackRoute;
        private void OnEnable()
        {
            if (stageDirector != null) stageDirector.StageReset += ResetRoute;
        }
        private void Start() { ResetRoute(); }
        private void ResetRoute()
        {
            usingBackRoute = false;
            if (walker != null)
            {
                walker.SetRoute(mainRoute);
                walker.ResetWalker();
            }
            if (hint != null) hint.text = "雨で本通りを消火・雷で左の柵を燃やす";
        }
        private void Update()
        {
            if (stageDirector == null || stageDirector.Phase != StagePhase.Playing ||
                usingBackRoute || fence == null) return;
            if (!fence.HasBurnedOut)
            {
                if (hint != null)
                {
                    if (fence.IsBurning) hint.text = "柵が燃焼中…燃え尽きると裏道が開通";
                    else if (fence.TryGetComponent(out WettableObject2D wet) && wet.IsWet)
                        hint.text = "柵が濡れている！乾くまで雷は待とう";
                    else hint.text = "雨で本通りを消火・雷で左の柵を燃やす";
                }
                return;
            }
            usingBackRoute = true;
            walker.SetRoute(backRoute);
            if (hint != null) hint.text = "裏道が開通！上の道から右の門へ";
        }
        private void OnDisable()
        {
            if (stageDirector != null) stageDirector.StageReset -= ResetRoute;
        }
    }
}
