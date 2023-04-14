using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    public abstract class GenericGravityEffect : TimedEffect
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

        Vector3 _overrideGravity;

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

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

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _overrideGravity = modifyGravity(Physics.gravity);
        }

        public override void OnStart()
        {
            Physics.gravity = _overrideGravity;

#if DEBUG
            Log.Debug($"New gravity: {_overrideGravity}");
#endif

            _hasGravityOverride = true;
        }

        public override void OnEnd()
        {
            tryRestoreGravity();
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
        static bool CanActivate(EffectCanActivateContext context)
        {
            if (!context.IsNow)
                return true;

            SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            return currentScene && Array.BinarySearch(_invalidOnScenes, currentScene.sceneDefIndex) < 0;
        }
    }
}
