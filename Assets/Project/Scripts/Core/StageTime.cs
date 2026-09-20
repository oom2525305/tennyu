using UnityEngine;

namespace Tenyu.Gameplay
{
    public static class StageTime
    {
        private const float AbilityDragScale = 1f / 3f;

        private static int dragCount;

        public static float Scale
        {
            get
            {
                return dragCount > 0
                    ? AbilityDragScale
                    : 1f;
            }
        }

        public static float DeltaTime
        {
            get
            {
                return Time.deltaTime * Scale;
            }
        }

        public static float FixedDeltaTime
        {
            get
            {
                return Time.fixedDeltaTime * Scale;
            }
        }

        public static void BeginAbilityDrag()
        {
            dragCount++;
        }

        public static void EndAbilityDrag()
        {
            dragCount = Mathf.Max(dragCount - 1, 0);
        }

        public static void Reset()
        {
            dragCount = 0;
        }
    }
}