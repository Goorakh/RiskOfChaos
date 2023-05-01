using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Gravity;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    public abstract class GenericGravityEffect : TimedEffect, IGravityModificationProvider
    {
        static SceneIndex[] _invalidOnScenes;

        [SystemInitializer(typeof(SceneCatalog))]
        static void InitScenes()
        {
            _invalidOnScenes = new SceneIndex[]
            {
                // Temporary fix: Changed gravity will potentially softlock due to the jump pads not going the correct height. Could also be solved by changing the applied force, but this works for now
                SceneCatalog.FindSceneIndex("moon"),
                SceneCatalog.FindSceneIndex("moon2")
            }.Where(static i => i != SceneIndex.Invalid).OrderBy(static i => i).ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            return currentScene && Array.BinarySearch(_invalidOnScenes, currentScene.sceneDefIndex) < 0;
        }

        public abstract event Action OnValueDirty;

        public abstract void ModifyValue(ref Vector3 gravity);

        public override void OnStart()
        {
            if (NetworkServer.active && GravityModificationManager.Instance)
            {
                GravityModificationManager.Instance.RegisterModificationProvider(this);
            }
        }

        public override void OnEnd()
        {
            if (NetworkServer.active && GravityModificationManager.Instance)
            {
                GravityModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
