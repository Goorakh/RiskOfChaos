using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using UnityEngine;

namespace RiskOfChaos.ConfigHandling
{
    public static class ConfigOptionFactory
    {
        public static BaseOption GetOption<T>(ConfigEntry<T> entry, BaseOptionConfig optionConfig)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            if (optionConfig is null)
                throw new ArgumentNullException(nameof(optionConfig));

            if (optionConfig is CheckBoxConfig checkBoxConfig)
            {
                if (entry is ConfigEntry<bool> boolEntry)
                {
                    return new CheckBoxOption(boolEntry, checkBoxConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else if (optionConfig is ChoiceConfig choiceConfig)
            {
                return new ChoiceOption(entry, choiceConfig);
            }
            else if (optionConfig is ColorOptionConfig colorOptionConfig)
            {
                if (entry is ConfigEntry<Color> colorEntry)
                {
                    return new ColorOption(colorEntry, colorOptionConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else if (optionConfig is InputFieldConfig inputFieldConfig)
            {
                if (entry is ConfigEntry<string> stringEntry)
                {
                    return new StringInputFieldOption(stringEntry, inputFieldConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else if (optionConfig is IntSliderConfig intSliderConfig)
            {
                if (entry is ConfigEntry<int> intEntry)
                {
                    return new IntSliderOption(intEntry, intSliderConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else if (optionConfig is KeyBindConfig keyBindConfig)
            {
                if (entry is ConfigEntry<KeyboardShortcut> keyboardShortcutEntry)
                {
                    return new KeyBindOption(keyboardShortcutEntry, keyBindConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else if (optionConfig is SliderConfig sliderConfig)
            {
                if (entry is ConfigEntry<float> floatEntry)
                {
                    return new SliderOption(floatEntry, sliderConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else if (optionConfig is StepSliderConfig stepSliderConfig)
            {
                if (entry is ConfigEntry<float> floatEntry)
                {
                    return new StepSliderOption(floatEntry, stepSliderConfig);
                }
                else
                {
                    Log.Error($"Invalid config entry type {entry.SettingType} for option config of type {optionConfig.GetType()}");
                    return null;
                }
            }
            else
            {
                Log.Error($"Unsupported option config type {optionConfig.GetType()}");
                return null;
            }
        }
    }
}
