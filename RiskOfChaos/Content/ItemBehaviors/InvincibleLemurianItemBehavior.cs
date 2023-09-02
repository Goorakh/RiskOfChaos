using RoR2;
using RoR2.Items;
using UnityEngine.Networking;

namespace RiskOfChaos.Content.ItemBehaviors
{
    public sealed class InvincibleLemurianItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = true)]
        static ItemDef GetItemDef()
        {
            return Items.InvincibleLemurianMarker;
        }

        string _originalNameToken;

        void OnEnable()
        {
            _originalNameToken = body ? body.baseNameToken : "UNKNOWN";
            body.baseNameToken = "INVINCIBLE_LEMURIAN_BODY_NAME";
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active || !body)
                return;

            if (!body.HasBuff(RoR2Content.Buffs.Immune))
            {
                body.AddBuff(RoR2Content.Buffs.Immune);
            }
        }

        void OnDisable()
        {
            if (body)
            {
                if (NetworkServer.active)
                {
                    if (body.HasBuff(RoR2Content.Buffs.Immune))
                    {
                        body.RemoveBuff(RoR2Content.Buffs.Immune);
                    }
                }

                body.baseNameToken = _originalNameToken;
            }
        }
    }
}
