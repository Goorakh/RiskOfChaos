using RiskOfChaos.Networking.Components;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(ItemStealController), typeof(SyncStolenItemCount))]
    public class ShowStolenItemsPositionIndicator : MonoBehaviour
    {
        ItemStealController _itemStealController;
        SyncStolenItemCount _syncedStolenItemCount;

        GameObject _positionIndicatorObject;
        PositionIndicator _positionIndicator;

        void Awake()
        {
            _itemStealController = GetComponent<ItemStealController>();
            _syncedStolenItemCount = GetComponent<SyncStolenItemCount>();
        }

        void OnEnable()
        {
            if (_itemStealController)
            {
                if (NetworkServer.active)
                {
                    _itemStealController.onStealFinishServer.AddListener(refreshShowPositionIndicator);
                }
                else
                {
                    _itemStealController.onStealFinishClient += refreshShowPositionIndicator;
                }
            }

            LocalUserManager.onLocalUsersUpdated += refreshShowPositionIndicator;

            refreshShowPositionIndicator();
        }

        void FixedUpdate()
        {
            // If the target object has been destroyed, remove the position indicator, otherwise a position indicator at (0, 0, 0) is left behind
            if (_positionIndicator && !_positionIndicator.targetTransform)
            {
                Destroy(_positionIndicatorObject);
            }
        }

        void OnDisable()
        {
            if (_itemStealController)
            {
                if (NetworkServer.active)
                {
                    _itemStealController.onStealFinishServer.RemoveListener(refreshShowPositionIndicator);
                }
                else
                {
                    _itemStealController.onStealFinishClient -= refreshShowPositionIndicator;
                }
            }

            LocalUserManager.onLocalUsersUpdated -= refreshShowPositionIndicator;

            if (_positionIndicator)
            {
                Destroy(_positionIndicatorObject);
            }
        }

        bool shouldShowPositionIndicator()
        {
            return _syncedStolenItemCount && LocalUserManager.readOnlyLocalUsersList.Any(user =>
            {
                if (user == null)
                    return false;

                CharacterMaster master = user.cachedMaster;
                if (!master)
                    return false;

                Inventory inventory = master.inventory;
                return inventory && _syncedStolenItemCount.GetStolenItemsCount(inventory) > 0;
            });
        }

        void refreshShowPositionIndicator()
        {
            if (shouldShowPositionIndicator())
            {
                if (!_positionIndicatorObject)
                {
                    if (_itemStealController.TryGetComponent(out NetworkedBodyAttachment itemStealControllerBodyAttachment) &&
                        itemStealControllerBodyAttachment.attachedBodyObject)
                    {
                        _positionIndicatorObject = Instantiate(NetPrefabs.ItemStealerPositionIndicatorPrefab);

                        if (_positionIndicatorObject.TryGetComponent(out _positionIndicator))
                        {
                            _positionIndicator.targetTransform = itemStealControllerBodyAttachment.attachedBodyObject.transform;
                        }
                        else
                        {
                            Log.Error("Position indicator is missing PositionIndicator component");
                        }
                    }
                    else
                    {
                        Log.Warning("Unable to find body object from ItemStealController");
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
