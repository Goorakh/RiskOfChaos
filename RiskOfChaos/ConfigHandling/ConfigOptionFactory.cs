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

            switch (optionConfig)
            {
                case CheckBoxConfig checkBoxConfig when entry is ConfigEntry<bool> boolEntry:
                    return new CheckBoxOption(boolEntry, checkBoxConfig);

                case ChoiceConfig choiceConfig:
                    return new ChoiceOption(entry, choiceConfig);

                case ColorOptionConfig colorOptionConfig when entry is ConfigEntry<Color> colorEntry:
                    return new ColorOption(colorEntry, colorOptionConfig);

                case FloatFieldConfig floatFieldConfig when entry is ConfigEntry<float> floatEntry:
                    return new FloatFieldOption(floatEntry, floatFieldConfig);

                case InputFieldConfig inputFieldConfig when entry is ConfigEntry<string> stringEntry:
                    return new StringInputFieldOption(stringEntry, inputFieldConfig);

                case IntFieldConfig intFieldConfig when entry is ConfigEntry<int> intEntry:
                    return new IntFieldOption(intEntry, intFieldConfig);

                case IntSliderConfig intSliderConfig when entry is ConfigEntry<int> intEntry:
                    return new IntSliderOption(intEntry, intSliderConfig);

                case KeyBindConfig keyBindConfig when entry is ConfigEntry<KeyboardShortcut> keyboardShortcutEntry:
                    return new KeyBindOption(keyboardShortcutEntry, keyBindConfig);

                case SliderConfig sliderConfig when entry is ConfigEntry<float> floatEntry:
                    return new SliderOption(floatEntry, sliderConfig);

                case StepSliderConfig stepSliderConfig when entry is ConfigEntry<float> floatEntry:
                    return new StepSliderOption(floatEntry, stepSliderConfig);

                default:
                    Log.Error($"Invalid config entry type {entry.SettingType.FullName} for option config of type {optionConfig.GetType().FullName}");
                    return null;
            }
        }
    }
}
