using RoR2;

namespace RiskOfChaos.ModificationController.HoldoutZone
{
    public interface IHoldoutZoneModificationProvider
    {
        HoldoutZoneModificationInfo GetHoldoutZoneModifications(HoldoutZoneController holdoutZone);
    }
}
