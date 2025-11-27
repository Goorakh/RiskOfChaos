using System;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    public sealed class ChaosEffectSubtitleComponent : MonoBehaviour
    {
        bool _subtitleProvidersDirty;
        IEffectSubtitleProvider[] _subtitleProviders = [];

        string _cachedSubtitle;
        public string Subtitle
        {
            get
            {
                return _cachedSubtitle;
            }
            set
            {
                if (string.Equals(_cachedSubtitle, value))
                    return;

                _cachedSubtitle = value;
                OnSubtitleChanged?.Invoke();
            }
        }

        public event Action OnSubtitleChanged;

        void Awake()
        {
            _subtitleProviders = GetComponents<IEffectSubtitleProvider>();
            foreach (IEffectSubtitleProvider subtitleProvider in _subtitleProviders)
            {
                subtitleProvider.OnSubtitleChanged += onSubtitleProviderChanged;
            }
        }

        void OnEnable()
        {
            refreshSubtitle();
        }

        void OnDestroy()
        {
            foreach (IEffectSubtitleProvider subtitleProvider in _subtitleProviders)
            {
                subtitleProvider.OnSubtitleChanged -= onSubtitleProviderChanged;
            }
        }

        void FixedUpdate()
        {
            if (_subtitleProvidersDirty)
            {
                _subtitleProvidersDirty = false;
                refreshSubtitle();
            }
        }

        void onSubtitleProviderChanged(IEffectSubtitleProvider subtitleProvider)
        {
            _subtitleProvidersDirty = true;
        }

        void refreshSubtitle()
        {
            StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

            foreach (IEffectSubtitleProvider subtitleProvider in _subtitleProviders)
            {
                Behaviour subtitleProviderBehavior = subtitleProvider as Behaviour;
                if (subtitleProviderBehavior && subtitleProviderBehavior.enabled)
                {
                    string subtitle = subtitleProvider.GetSubtitle();
                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        if (stringBuilder.Length > 0)
                        {
                            stringBuilder.Append('\n');
                        }

                        stringBuilder.Append(subtitle);
                    }
                }
            }

            string combinedSubtitle = stringBuilder.ToString();
            stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);

            Subtitle = combinedSubtitle.Trim();
        }
    }
}
