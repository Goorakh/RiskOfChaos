using RoR2;

namespace RiskOfChaos.Utilities
{
    public readonly struct PickupTryGrantResult
    {
        public static readonly PickupTryGrantResult Failed = new PickupTryGrantResult(ResultState.Failed, PickupIndex.none);
        public static readonly PickupTryGrantResult CompleteSuccess = new PickupTryGrantResult(ResultState.CompleteSuccess, PickupIndex.none);

        public enum ResultState : byte
        {
            Failed,
            PartialSuccess,
            CompleteSuccess
        }
        public readonly ResultState State;

        public readonly PickupIndex PickupToSpawn;

        PickupTryGrantResult(ResultState resultState, PickupIndex pickupToSpawn)
        {
            State = resultState;
            PickupToSpawn = pickupToSpawn;
        }

        public static PickupTryGrantResult PartialSuccess(PickupIndex pickupToSpawn)
        {
            return new PickupTryGrantResult(ResultState.PartialSuccess, pickupToSpawn);
        }
    }
}
