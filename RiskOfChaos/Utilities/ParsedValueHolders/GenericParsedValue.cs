using BepInEx.Configuration;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.ParsedValueHolders
{
    public abstract class GenericParsedValue<T>
    {
        protected ConfigEntry<string> _boundToConfig;

        string _lastParsedInput = null;
        public string ParsedInput
        {
            get
            {
                return _lastParsedInput;
            }
            set
            {
                value = value?.TrimEnd('\r', '\n');

                if (string.Equals(value, _lastParsedInput))
                    return;

                _lastParsedInput = value;

                if (_parseReady)
                {
                    setParsedValue(value);
                }
            }
        }

        bool _parseReady = true;
        public bool ParseReady
        {
            get
            {
                return _parseReady;
            }
            set
            {
                if (_parseReady == value)
                    return;

                _parseReady = value;

                if (_parseReady && !string.IsNullOrWhiteSpace(_lastParsedInput))
                {
                    setParsedValue(_lastParsedInput);
                }
                else
                {
                    resetValue();
                }
            }
        }

        public ParseFailReason ParseFailReason { get; private set; }

        public ParsedValueState ValueState { get; private set; } = ParsedValueState.Unassigned;

        public bool HasParsedValue => ValueState == ParsedValueState.Valid;
        T _parsedValue;

        public GenericParsedValue()
        {
        }

        ~GenericParsedValue()
        {
            if (_boundToConfig != null)
            {
                _boundToConfig.SettingChanged -= onBoundConfigChanged;
            }
        }

        public void ForceRefreshValue()
        {
            setParsedValue(_lastParsedInput);
        }

        public void BindToConfig(ConfigEntry<string> entry)
        {
            if (_boundToConfig != null)
            {
                _boundToConfig.SettingChanged -= onBoundConfigChanged;
            }

            _boundToConfig = entry;
            _boundToConfig.SettingChanged += onBoundConfigChanged;
            ParsedInput = _boundToConfig.Value;
        }

        void onBoundConfigChanged(object sender, EventArgs e)
        {
            if (sender is ConfigEntry<string> config)
            {
                ParsedInput = config.Value;
            }
            else
            {
                Log.Warning($"Sender {sender} is not of type {nameof(ConfigEntry<string>)}");
            }
        }

        public T GetValue(T fallback)
        {
            return HasParsedValue ? _parsedValue : fallback;
        }

        public bool TryGetValue(out T parsedValue)
        {
            parsedValue = _parsedValue;
            return HasParsedValue;
        }

        protected TValue handleParsedInput<TValue>(string input, Func<string, TValue> parseFunc)
        {
            try
            {
                return parseFunc(input);
            }
            catch (ParseException)
            {
                try
                {
                    return parseFunc(input.Trim());
                }
                catch (ParseException trimEx)
                {
                    throw trimEx;
                }
            }
        }

        protected abstract T parseInput(string input);

        public virtual IEnumerable<ParseFailReason> GetAllParseFailReasons()
        {
            if (ValueState == ParsedValueState.ParseFailed)
            {
                yield return ParseFailReason;
            }
        }

        void setParsedValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                resetValue();
                return;
            }

            try
            {
                _parsedValue = handleParsedInput(value, parseInput);

#if DEBUG
                if (_boundToConfig != null)
                {
                    Log.Debug($"Successfully parsed value of {_boundToConfig.Definition} (\"{value}\"): {_parsedValue}");
                }
                else
                {
                    Log.Debug($"Successfully parsed \"{value}\": {_parsedValue}");
                }
#endif

                ParseFailReason = null;
                ValueState = ParsedValueState.Valid;
            }
            catch (ParseException ex)
            {
                if (_boundToConfig != null)
                {
                    Log.Error($"Unable to parse value of {_boundToConfig.Definition} (\"{value}\"): {ex.Message}");
                }
                else
                {
                    Log.Error($"Unable to parse value \"{value}\": {ex.Message}");
                }

                ParseFailReason = new ParseFailReason(value, ex);
                ValueState = ParsedValueState.ParseFailed;
                _parsedValue = default;
            }
        }

        void resetValue()
        {
            _parsedValue = default;
            ParseFailReason = null;
            ValueState = ParsedValueState.Unassigned;
        }
    }
}
