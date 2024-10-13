using RiskOfChaos.Components;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.OLD_ModifierController
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ValueModificationManagerAttribute : HG.Reflection.SearchableAttribute
    {
        readonly Type[] _additionalComponents;

        public bool DontDestroyOnLoad { get; set; } = true;

        public bool DestroyOnRunEnd { get; set; } = true;

        public new Type target => base.target as Type;

        public ValueModificationManagerAttribute(params Type[] additionalComponents)
        {
            _additionalComponents = additionalComponents;
        }

        public IEnumerable<Type> GetAdditionalComponentTypes()
        {
            if (DontDestroyOnLoad)
                yield return typeof(SetDontDestroyOnLoad);

            if (DestroyOnRunEnd)
                yield return typeof(DestroyOnRunEnd);

            foreach (Type componentType in _additionalComponents)
            {
                yield return componentType;
            }
        }
    }
}
