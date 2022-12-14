using BepInEx.Configuration;
using RiskOfChaos.EffectDefinitions;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Linq;
using System.Reflection;

namespace RiskOfChaos.EffectHandling
{
    public readonly struct ChaosEffectInfo
    {
        public readonly int EffectIndex;

        public readonly string Identifier;

        public readonly string NameToken;
        // public readonly string DescriptionToken;

        public readonly Type EffectType;

        public readonly string ConfigSectionName;

        public readonly ConfigEntry<bool> IsEnabledConfig;
        public readonly ConfigEntry<float> SelectionWeightConfig;

        readonly MethodInfo[] _canActivateMethods;
        public bool CanActivate => _canActivateMethods != null && (_canActivateMethods.Length == 0 || _canActivateMethods.All(m => (bool)m.Invoke(null, null)));

        readonly MethodInfo[] _weightMultSelectorMethods;
        public float TotalSelectionWeight
        {
            get
            {
                float weight = SelectionWeightConfig.Value;

                if (_weightMultSelectorMethods != null)
                {
                    foreach (MethodInfo weightSelector in _weightMultSelectorMethods)
                    {
                        weight *= (float)weightSelector.Invoke(null, null);
                    }
                }

                return weight;
            }
        }

        public ChaosEffectInfo(int effectIndex, ChaosEffectAttribute attribute)
        {
            const string LOG_PREFIX = $"{nameof(ChaosEffectInfo)}..ctor ";

            EffectIndex = effectIndex;
            Identifier = attribute.Identifier;

            NameToken = $"EFFECT_{Identifier.ToUpper()}_NAME";
            // DescriptionToken = attribute.HasDescription ? $"EFFECT_{Identifier.ToUpper()}_DESC" : null;

            if (attribute.target is Type effectType)
            {
                EffectType = effectType;

                if (!typeof(BaseEffect).IsAssignableFrom(effectType))
                {
                    Log.Error(LOG_PREFIX + $"effect type {effectType.FullName} is not {nameof(BaseEffect)}");
                }
                else
                {
                    _canActivateMethods = effectType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Where(m => m.GetParameters().Length == 0 && m.GetCustomAttribute<EffectCanActivateAttribute>() != null).ToArray();

                    _weightMultSelectorMethods = effectType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Where(m => m.GetCustomAttribute<EffectWeightMultiplierSelectorAttribute>() != null).ToArray();
                }
            }
            else
            {
                Log.Error(LOG_PREFIX + $"attribute target is not a Type ({attribute.target})");
            }

            ConfigSectionName = "Effect: " + Language.GetString(NameToken, "en");

            IsEnabledConfig = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Enabled"), true, new ConfigDescription("If the effect should be able to be picked"));
            ModSettingsManager.AddOption(new CheckBoxOption(IsEnabledConfig), ChaosEffectCatalog.CONFIG_MOD_GUID, ChaosEffectCatalog.CONFIG_MOD_NAME);

            SelectionWeightConfig = Main.Instance.Config.Bind(new ConfigDefinition(ConfigSectionName, "Effect Weight"), attribute.DefaultSelectionWeight, new ConfigDescription("How likely the effect is to be picked, higher value means more likely, lower value means less likely"));
            ModSettingsManager.AddOption(new StepSliderOption(SelectionWeightConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                increment = 0.1f,
                min = 0f,
                max = 2.5f
            }), ChaosEffectCatalog.CONFIG_MOD_GUID, ChaosEffectCatalog.CONFIG_MOD_NAME);
        }

        public readonly BaseEffect InstantiateEffect(Xoroshiro128Plus effectRNG)
        {
            BaseEffect effectInstance = (BaseEffect)Activator.CreateInstance(EffectType);
            effectInstance.RNG = effectRNG;
            return effectInstance;
        }

        public readonly string GetActivationMessage()
        {
            return Language.GetStringFormatted("CHAOS_EFFECT_ACTIVATE", Language.GetString(NameToken));
        }
    }
}
