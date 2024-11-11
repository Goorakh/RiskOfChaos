using RiskOfChaos.Content;
using RiskOfChaos.Networking.Components;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [RequiredComponents(typeof(SyncStolenItemCount))]
    public class ShowStolenItemsPositionIndicator : MonoBehaviour
    {
        NetworkedBodyAttachment _bodyAttachment;

        SyncStolenItemCount _syncedStolenItemCount;

        GameObject _positionIndicatorObject;
        PositionIndicator _positionIndicator;

        void Awake()
        {
            _bodyAttachment = GetComponent<NetworkedBodyAttachment>();
            _syncedStolenItemCount = GetComponent<SyncStolenItemCount>();
        }

        void FixedUpdate()
        {
            refreshShowPositionIndicator();
        }

        void OnDisable()
        {
            if (_positionIndicatorObject)
            {
                Destroy(_positionIndicatorObject);
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

        void refreshShowPositionIndicator()
        {
            if (shouldShowPositionIndicator())
            {
                if (!_positionIndicatorObject)
                {
                    if (_bodyAttachment.attachedBody)
                    {
                        _positionIndicatorObject = Instantiate(RoCContent.LocalPrefabs.ItemStealerPositionIndicator);

                        _positionIndicator = _positionIndicatorObject.GetComponent<PositionIndicator>();
                        _positionIndicator.targetTransform = _bodyAttachment.attachedBody.coreTransform;
                    }
                }
                else if (!_positionIndicatorObject.activeSelf)
                {
                    _positionIndicatorObject.SetActive(true);
                }
            }
            else
            {
                if (_positionIndicatorObject)
                {
                    _positionIndicatorObject.SetActive(false);
                }
            }
        }
    }
}
