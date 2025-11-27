using RiskOfChaos.Content;
using RiskOfChaos.EffectDefinitions.Character.Buff;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    [RequiredComponents(typeof(ApplyBuffEffect), typeof(ChaosEffectSubtitleComponent))]
    public sealed class BuffSubtitleProvider : MonoBehaviour, IEffectSubtitleProvider
    {
        ApplyBuffEffect _applyBuffEffect;

        string _cachedSubtitle;

        public event Action<IEffectSubtitleProvider> OnSubtitleChanged;

        void Awake()
        {
            _applyBuffEffect = GetComponent<ApplyBuffEffect>();
        }

        void OnEnable()
        {
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
            _applyBuffEffect.OnAppliedBuffChanged += onAppliedBuffChanged;
            markSubtitleDirty();
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
            _applyBuffEffect.OnAppliedBuffChanged -= onAppliedBuffChanged;
        }

        void onCurrentLanguageChanged()
        {
            markSubtitleDirty();
        }

        void onAppliedBuffChanged()
        {
            markSubtitleDirty();
        }

        void markSubtitleDirty()
        {
            _cachedSubtitle = null;
            OnSubtitleChanged?.Invoke(this);
        }

        string generateSubtitle()
        {
            if (!_applyBuffEffect)
                return string.Empty;

            BuffIndex buffIndex = _applyBuffEffect.BuffIndex;

            BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
            if (!buffDef)
                return string.Empty;

            string buffName = BuffUtils.GetLocalizedBuffName(buffDef);
            if (string.IsNullOrWhiteSpace(buffName))
                return string.Empty;

            StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

            stringBuilder.Append("(");

            Color buffColor = buffDef.buffColor;
            if (buffDef.isElite && buffDef.eliteDef)
            {
                Color32 eliteColor = buffDef.eliteDef.color;
                if (eliteColor.r > 0 || eliteColor.g > 0 || eliteColor.b > 0)
                {
                    buffColor = eliteColor;
                }
            }

            Color.RGBToHSV(buffColor, out float h, out float s, out float v);
            v = Mathf.Max(0.5f, v);
            buffColor = Color.HSVToRGB(h, s, v);

            stringBuilder.Append("<color=#").AppendColor32RGBHexValues(buffColor).Append(">");

            stringBuilder.Append(buffName);

            stringBuilder.Append("</color>");

            int buffStackCount = _applyBuffEffect.BuffStackCount;
            if (buffStackCount != 1)
            {
                stringBuilder.Append(" x").Append(buffStackCount);
            }

            stringBuilder.Append(")");

            string subtitle = stringBuilder.ToString();
            stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
            return subtitle;
        }

        public string GetSubtitle()
        {
            _cachedSubtitle ??= generateSubtitle();
            return _cachedSubtitle;
        }
    }
}
