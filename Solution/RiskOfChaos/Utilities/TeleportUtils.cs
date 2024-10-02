using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities
{
    public static class TeleportUtils
    {
        public static void TeleportBody(CharacterBody body, Vector3 targetPosition)
        {
            if (!body.hasEffectiveAuthority)
            {
#if DEBUG
                Log.Debug($"No authority over {body}, sending net message");
#endif

                NetworkIdentity bodyNetIdentity = body.networkIdentity;
                NetworkConnection targetConnection = bodyNetIdentity.clientAuthorityOwner ?? (NetworkServer.active ? bodyNetIdentity.connectionToClient : bodyNetIdentity.connectionToServer);
                if (targetConnection != null)
                {
                    new TeleportBodyMessage(body.gameObject, targetPosition).Send(targetConnection);
                }
                else
                {
                    Log.Warning("Body does not have authority, but has no network connection");
                }

                return;
            }

            TeleportHelper.TeleportBody(body, targetPosition);

            GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(body.gameObject);
            if (teleportEffectPrefab)
            {
                EffectManager.SimpleEffect(teleportEffectPrefab, targetPosition, Quaternion.identity, true);
            }
        }
    }
}
