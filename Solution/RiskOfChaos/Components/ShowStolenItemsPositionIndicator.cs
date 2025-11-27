using RiskOfChaos.Content;
using RiskOfChaos.Networking.Components;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [RequiredComponents(typeof(NetworkedBodyAttachment), typeof(SyncStolenItemCount))]
    public sealed class ShowStolenItemsPositionIndicator : MonoBehaviour
    {
        NetworkedBodyAttachment _bodyAttachment;

        SyncStolenItemCount _syncedStolenItemCount;

        PositionIndicator _positionIndicator;

        float _refreshIndicatorTimer;
        const float INDICATOR_REFRESH_INTERVAL = 1.5f;

        void Awake()
        {
            _bodyAttachment = GetComponent<NetworkedBodyAttachment>();
            _syncedStolenItemCount = GetComponent<SyncStolenItemCount>();
        }

        void OnEnable()
        {
            refreshPositionIndicator();
        }

        void OnDisable()
        {
            if (_positionIndicator)
            {
                Destroy(_positionIndicator.gameObject);
                _positionIndicator = null;
            }
        }

        void FixedUpdate()
        {
            _refreshIndicatorTimer -= Time.fixedDeltaTime;
            if (_refreshIndicatorTimer <= 0f)
            {
                _refreshIndicatorTimer = INDICATOR_REFRESH_INTERVAL;
                refreshPositionIndicator();
            }
        }

        bool shouldShowPositionIndicator()
        {
            CharacterBody body = _bodyAttachment.attachedBody;
            if (!body || !body.gameObject.activeInHierarchy)
                return false;

            HealthComponent healthComponent = body.healthComponent;
            if (healthComponent && !healthComponent.alive)
                return false;

            foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
            {
                CharacterMaster userMaster = user.cachedMaster;
                if (!userMaster)
                    continue;

                Inventory userInventory = userMaster.inventory;
                if (!userInventory)
                    continue;

                if (_syncedStolenItemCount.GetStolenItemsCount(userInventory) > 0)
                    return true;
            }

            return false;
        }

        void refreshPositionIndicator()
        {
            setIndicatorVisible(shouldShowPositionIndicator());

            if (_positionIndicator && _positionIndicator.gameObject.activeInHierarchy && !_positionIndicator.targetTransform && _bodyAttachment.attachedBody)
            {
                _positionIndicator.targetTransform = _bodyAttachment.attachedBody.coreTransform;
            }
        }

        void setIndicatorVisible(bool active)
        {
            if (_positionIndicator)
            {
                _positionIndicator.gameObject.SetActive(active);
            }
            else if (active)
            {
                if (_bodyAttachment.attachedBody)
                {
                    GameObject positionIndicatorObject = Instantiate(RoCContent.LocalPrefabs.ItemStealerPositionIndicator);

                    _positionIndicator = positionIndicatorObject.GetComponent<PositionIndicator>();
                    _positionIndicator.targetTransform = _bodyAttachment.attachedBody.coreTransform;
                }
            }
        }
    }
}
