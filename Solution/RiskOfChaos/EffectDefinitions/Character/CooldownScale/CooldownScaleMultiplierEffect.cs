using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.CooldownScale
{
    public class CooldownScaleMultiplierEffect : NetworkBehaviour, ISkillSlotModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance;
        }

        float _multiplier = 1f;
        public float Multiplier
        {
            get
            {
                return _multiplier;
            }

            [Server]
            set
            {
                if (_multiplier == value)
                    return;

                _multiplier = value;
                OnValueDirty?.Invoke();
            }
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData value)
        {
            value.CooldownScale *= Multiplier;
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                SkillSlotModificationManager.Instance.RegisterModificationProvider(this);
            }
        }

        void OnDestroy()
        {
            if (SkillSlotModificationManager.Instance)
            {
                SkillSlotModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
