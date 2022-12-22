using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    public abstract class GenericMultiplyGravityEffect : BaseEffect
    {
        protected abstract float multiplier { get; }

        static bool _hasGravityChangedThisStage = false;

        static bool _hasRestoreEvent = false;
        static void tryAddRestoreEventListener()
        {
            if (_hasRestoreEvent)
                return;

#if DEBUG
            Log.Debug($"{nameof(GenericMultiplyGravityEffect)} adding reset gravity event listener");
#endif

            Stage.onServerStageComplete += static _ =>
            {
                tryResetGravity();
            };

            // onServerStageComplete doesn't happen if the run is lost or exited prematurely, so make sure to reset the gravity regardless of how the stage ends
            Run.onRunDestroyGlobal += static _ =>
            {
                tryResetGravity();
            };

            _hasRestoreEvent = true;
        }

        static void tryResetGravity()
        {
            if (_hasGravityChangedThisStage)
            {
#if DEBUG
                Log.Debug($"{nameof(GenericMultiplyGravityEffect)} resetting gravity");
#endif

                Physics.gravity = new Vector3(0f, Run.baseGravity, 0f);

                _hasGravityChangedThisStage = false;
            }
        }

        public override void OnStart()
        {
            Physics.gravity *= multiplier;
            _hasGravityChangedThisStage = true;
            tryAddRestoreEventListener();
        }

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
    }
}
