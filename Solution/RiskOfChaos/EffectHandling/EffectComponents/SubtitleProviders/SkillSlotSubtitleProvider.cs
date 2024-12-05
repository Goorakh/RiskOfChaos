using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    [RequiredComponents(typeof(ChaosEffectSubtitleComponent))]
    public class SkillSlotSubtitleProvider : NetworkBehaviour, IEffectSubtitleProvider
    {
        [SyncVar(hook = nameof(hookSetSkillSlot))]
        int _skillSlotInternal;

        public SkillSlot SkillSlot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (SkillSlot)(_skillSlotInternal - 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _skillSlotInternal = (int)value + 1;
        }

        string _cachedSubtitle;

        public event Action<IEffectSubtitleProvider> OnSubtitleChanged;

        public override void OnStartClient()
        {
            base.OnStartClient();
            markSubtitleDirty();
        }

        void OnEnable()
        {
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
            markSubtitleDirty();
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
        }

        public string GetSubtitle()
        {
            _cachedSubtitle ??= generateSubtitle();
            return _cachedSubtitle;
        }

        string generateSubtitle()
        {
            string slotNameToken = SkillSlotUtils.GetSkillSlotNameToken(SkillSlot);
            if (string.IsNullOrWhiteSpace(slotNameToken))
                return string.Empty;

            return $"({Language.GetString(slotNameToken)})";
        }

        void onCurrentLanguageChanged()
        {
            markSubtitleDirty();
        }

        void markSubtitleDirty()
        {
            _cachedSubtitle = null;
            OnSubtitleChanged?.Invoke(this);
        }

        void hookSetSkillSlot(int skillSlotInt)
        {
            _skillSlotInternal = skillSlotInt;
            markSubtitleDirty();
        }
    }
}
