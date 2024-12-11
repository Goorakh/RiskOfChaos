using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class WorldUtils
    {
        public static Vector3 GetWorldUpByGravity(Vector3 up)
        {
            Vector3 gravity = Physics.gravity;
            if (gravity.sqrMagnitude == 0f)
                return up;

            return -gravity.normalized;
        }

        public static Vector3 GetWorldUpByGravity()
        {
            return GetWorldUpByGravity(Vector3.up);
        }
    }
}
