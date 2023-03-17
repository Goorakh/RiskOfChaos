using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Networking;
using RoR2;
using System;
using System.Linq;
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
            Log.Debug("adding reset gravity event listener");
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
                Log.Debug("resetting gravity");
#endif

                // Don't network this setter, since it's either called at run destroy, or end of start (in which case it might get received too late and override the stage gravity), _hasGravityChangedThisStage is networked anyway
                Physics.gravity = new Vector3(0f, Run.baseGravity, 0f);

                _hasGravityChangedThisStage = false;
            }
        }

        static void onGravityChanged()
        {
            _hasGravityChangedThisStage = true;
            tryAddRestoreEventListener();
        }

        public override void OnStart()
        {
            SyncSetGravity.NetworkedGravity *= multiplier;
            onGravityChanged();
        }

        [SystemInitializer]
        static void InitNetworkEventListener()
        {
            SyncSetGravity.OnReceive += SyncSetGravity_OnReceive;
        }

        static void SyncSetGravity_OnReceive(in Vector3 newGravity)
        {
            onGravityChanged();
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
