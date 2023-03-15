using RoR2;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class TeleportUtils
    {
        public static void TeleportBody(CharacterBody body, Vector3 targetPosition)
        {
            TeleportHelper.TeleportBody(body, targetPosition);

            GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(body.gameObject);
            if (teleportEffectPrefab)
            {
                EffectManager.SimpleEffect(teleportEffectPrefab, targetPosition, Quaternion.identity, true);
            }
        }
    }
}
