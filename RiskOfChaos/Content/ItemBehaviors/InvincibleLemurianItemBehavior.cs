using RoR2;
using RoR2.Items;

namespace RiskOfChaos.Content.ItemBehaviors
{
    public sealed class InvincibleLemurianItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        static ItemDef GetItemDef()
        {
            return Items.InvincibleLemurianMarker;
        }

        void FixedUpdate()
        {
            if (!body)
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
                if (body.HasBuff(RoR2Content.Buffs.Immune))
                {
                    body.RemoveBuff(RoR2Content.Buffs.Immune);
                }
            }
        }
    }
}
