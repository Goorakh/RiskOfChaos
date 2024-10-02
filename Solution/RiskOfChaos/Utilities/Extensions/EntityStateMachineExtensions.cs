using EntityStates;
using RoR2;
using System;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class EntityStateMachineExtensions
    {
        public static bool CurrentStateInheritsFrom(this EntityStateMachine esm, SerializableEntityStateType serializableStateType)
        {
            return esm.CurrentStateInheritsFrom(serializableStateType.stateType);
        }

        public static bool CurrentStateInheritsFrom(this EntityStateMachine esm, Type stateType)
        {
            if (stateType is null)
                throw new ArgumentNullException(nameof(stateType));

            return esm && esm.state != null && stateType.IsAssignableFrom(esm.state.GetType());
        }
    }
}
