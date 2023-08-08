using HarmonyLib;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [IncompatibleEffects(typeof(GenericAttractPickupsEffect))]
    public abstract class GenericAttractPickupsEffect : TimedEffect
    {
        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.PickupDropletController.Start += (orig, self) =>
            {
                orig(self);
                _onPickupCreated?.Invoke(self);
            };

            On.RoR2.GenericPickupController.Start += (orig, self) =>
            {
                orig(self);
                _onPickupCreated?.Invoke(self);
            };

            On.RoR2.PickupPickerController.Awake += (orig, self) =>
            {
                orig(self);
                _onPickupCreated?.Invoke(self);
            };

            _hasAppliedPatches = true;
        }

        static event Action<MonoBehaviour> _onPickupCreated;

        readonly HashSet<AttractToPlayers> _createdInstances = new HashSet<AttractToPlayers>();

        public override void OnStart()
        {
            tryApplyPatches();

            GameObject.FindObjectsOfType<PickupDropletController>().TryDo(tryAddComponentTo);
            GameObject.FindObjectsOfType<GenericPickupController>().TryDo(tryAddComponentTo);
            GameObject.FindObjectsOfType<PickupPickerController>().TryDo(tryAddComponentTo);

            _onPickupCreated += tryAddComponentTo;
        }

        public override void OnEnd()
        {
            _createdInstances.Do(GameObject.Destroy);
            _createdInstances.Clear();

            _onPickupCreated -= tryAddComponentTo;
        }

        void tryAddComponentTo(MonoBehaviour self)
        {
            AttractToPlayers attractToPlayers = AttractToPlayers.TryAddComponent(self);
            if (attractToPlayers)
            {
                _createdInstances.Add(attractToPlayers);
                onAttractorComponentAdded(attractToPlayers);
            }
        }

        protected abstract void onAttractorComponentAdded(AttractToPlayers attractToPlayers);
    }
}
