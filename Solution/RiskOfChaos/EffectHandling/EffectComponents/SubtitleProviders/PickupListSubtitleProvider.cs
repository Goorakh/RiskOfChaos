using RiskOfChaos.Content;
using RiskOfChaos.Networking.SyncList;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    [RequiredComponents(typeof(ChaosEffectSubtitleComponent))]
    public sealed class PickupListSubtitleProvider : NetworkBehaviour, IEffectSubtitleProvider
    {
        readonly SyncListPickupIndex _pickups = [];

        string _cachedSubtitle;

        public event Action<IEffectSubtitleProvider> OnSubtitleChanged;

        void Awake()
        {
            _pickups.Callback = pickupsSyncListCallback;
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
        public void AddPickup(PickupIndex pickupIndex)
        {
            _pickups.Add(pickupIndex);
        }

        [Server]
        public void RemovePickup(PickupIndex pickupIndex)
        {
            _pickups.Remove(pickupIndex);
        }

        [Server]
        public void ClearPickups()
        {
            _pickups.Clear();
        }

        void pickupsSyncListCallback(SyncList<PickupIndex>.Operation op, int itemIndex)
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
            if (_pickups.Count == 0)
                return string.Empty;

            StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

            stringBuilder.Append("(");

            for (int i = 0; i < _pickups.Count; i++)
            {
                string pickupNameToken = PickupCatalog.invalidPickupToken;
                Color pickupColor = PickupCatalog.invalidPickupColor;

                PickupDef pickupDef = PickupCatalog.GetPickupDef(_pickups[i]);
                if (pickupDef != null)
                {
                    pickupNameToken = pickupDef.nameToken;
                    pickupColor = pickupDef.baseColor;
                }

                stringBuilder.AppendColoredString(Language.GetString(pickupNameToken), pickupColor);

                if (i != _pickups.Count - 1)
                {
                    stringBuilder.Append(", ");
                }
            }

            stringBuilder.Append(")");

            string subtitle = stringBuilder.ToString();
            stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);

            return subtitle;
        }
    }
}
