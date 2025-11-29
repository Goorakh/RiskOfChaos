using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public sealed class KeepDisabled : MonoBehaviour
    {
        static readonly List<GameObject> _objectsToReenable = [];

        public ChaosEffectComponent OwnerEffectComponent
        {
            get => field;
            set
            {
                if (field == value)
                    return;

                if (field)
                {
                    field.OnEffectEnd -= onOwnerEffectEnd;
                }

                field = value;

                if (field)
                {
                    field.OnEffectEnd += onOwnerEffectEnd;
                }
            }
        }

        void OnEnable()
        {
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_objectsToReenable.Count == 0)
            {
                RoR2Application.onNextUpdate += reenableObjects;
            }

            _objectsToReenable.Add(gameObject);
        }

        void onOwnerEffectEnd(ChaosEffectComponent effectComponent)
        {
            Destroy(this);
        }

        static void reenableObjects()
        {
            foreach (GameObject gameObject in _objectsToReenable)
            {
                if (gameObject)
                {
                    Log.Debug($"Re-activated {gameObject}");
                    gameObject.SetActive(true);
                }
            }

            _objectsToReenable.Clear();
        }
    }
}
