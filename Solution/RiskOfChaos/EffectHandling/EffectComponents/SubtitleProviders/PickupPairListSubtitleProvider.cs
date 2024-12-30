using RiskOfChaos.Content;
using RiskOfChaos.Networking.SyncList;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    [RequiredComponents(typeof(ChaosEffectSubtitleComponent))]
    public class PickupPairListSubtitleProvider : NetworkBehaviour, IEffectSubtitleProvider
    {
        public string PairFormatToken;

        readonly SyncListPickupPair _pickupPairs = [];

        string _cachedSubtitle;

        public event Action<IEffectSubtitleProvider> OnSubtitleChanged;

        void Awake()
        {
            _pickupPairs.Callback = pickupPairsSyncListCallback;
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

        [Server]
        public void AddPair(PickupPair pair)
        {
            _pickupPairs.Add(pair);
        }

        [Server]
        public void RemovePair(PickupPair pair)
        {
            _pickupPairs.Remove(pair);
        }

        [Server]
        public void ClearPairs()
        {
            _pickupPairs.Clear();
        }

        void pickupPairsSyncListCallback(SyncList<PickupPair>.Operation op, int itemIndex)
        {
            markSubtitleDirty();
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

        public string GetSubtitle()
        {
            _cachedSubtitle ??= generateSubtitle();
            return _cachedSubtitle;
        }

        string generateSubtitle()
        {
            if (_pickupPairs.Count == 0)
                return string.Empty;

            StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

            for (int i = 0; i < _pickupPairs.Count; i++)
            {
                PickupPair pair = _pickupPairs[i];

                Color fromPickupColor = PickupCatalog.invalidPickupColor;
                string fromPickupNameToken = PickupCatalog.invalidPickupToken;
                PickupDef fromPickup = PickupCatalog.GetPickupDef(pair.PickupA);
                if (fromPickup != null)
                {
                    fromPickupColor = fromPickup.baseColor;
                    fromPickupNameToken = fromPickup.nameToken;
                }

                Color toPickupColor = PickupCatalog.invalidPickupColor;
                string toPickupNameToken = PickupCatalog.invalidPickupToken;
                PickupDef toPickup = PickupCatalog.GetPickupDef(pair.PickupB);
                if (toPickup != null)
                {
                    toPickupColor = toPickup.baseColor;
                    toPickupNameToken = toPickup.nameToken;
                }

                string fromPickupColored = Util.GenerateColoredString(Language.GetString(fromPickupNameToken), fromPickupColor);
                string toPickupColored = Util.GenerateColoredString(Language.GetString(toPickupNameToken), toPickupColor);

                string pairFormat = "{0} {1}";
                if (!string.IsNullOrWhiteSpace(PairFormatToken) && !Language.IsTokenInvalid(PairFormatToken))
                {
                    pairFormat = Language.GetString(PairFormatToken);
                }

                stringBuilder.AppendFormat(pairFormat, fromPickupColored, toPickupColored);

                if (i != _pickupPairs.Count - 1)
                {
                    stringBuilder.Append('\n');
                }
            }

            string subtitle = stringBuilder.ToString();
            stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);

            return subtitle;
        }
    }
}
