using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Networking;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    public abstract class GenericGravityEffect : BaseEffect
    {
        protected abstract Vector3 modifyGravity(Vector3 originalGravity);

        static bool _hasGravityOverride = false;

        public static bool AnyGravityChangeActive => _hasGravityOverride;

        static void tryRestoreGravity()
        {
            if (!_hasGravityOverride)
                return;

#if DEBUG
            Log.Debug("Restoring gravity");
#endif

            Physics.gravity = new Vector3(0f, Run.baseGravity, 0f);
            _hasGravityOverride = false;
        }

        static bool _hasAddedEventListeners = false;
        static void tryAddEventListeners()
        {
            if (_hasAddedEventListeners)
                return;

            Run.onRunDestroyGlobal += _ =>
            {
                tryRestoreGravity();
            };

            StageCompleteMessage.OnReceive += _ =>
            {
                tryRestoreGravity();
            };

            _hasAddedEventListeners = true;
        }

        Vector3 _overrideGravity;

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_overrideGravity);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _overrideGravity = reader.ReadVector3();
        }

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                _overrideGravity = modifyGravity(Physics.gravity);
            }

            Physics.gravity = _overrideGravity;
            _hasGravityOverride = true;

            tryAddEventListeners();
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
