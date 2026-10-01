using UnityEngine;
using TMPro;

namespace Tenyu.Gameplay
{
    // Only presents the stage state. Route selection belongs to the walker's pathfinder.
    public sealed class BurningTownRoute2D : MonoBehaviour
    {
        [SerializeField] private StageDirector stageDirector;
        [SerializeField] private FlammableObject2D fence;
        [SerializeField] private TMP_Text hint;
        private void Update()
        {
            if (stageDirector == null || hint == null || fence == null ||
                stageDirector.Phase == StagePhase.Paused) return;
            if (fence.HasBurnedOut) hint.text = "裏道が開通！風で誘導・柵で本通りをふさぐ";
            else if (fence.IsBurning) hint.text = "柵が燃焼中…開通後も進路は自分で選ぶ";
            else if (fence.TryGetComponent(out WettableObject2D wet) && wet.IsWet)
                hint.text = "柵が濡れている！乾くまで雷は待とう";
            else hint.text = "雨で本通りを消火・雷で上の柵を開く";
        }
    }
}
