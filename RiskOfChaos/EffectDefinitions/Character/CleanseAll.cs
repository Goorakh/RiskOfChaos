using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("cleanse_all")]
    public sealed class CleanseAll : BaseEffect
    {
        static GameObject _cleanseEffectPrefab;

        [SystemInitializer]
        static void Init()
        {
            _cleanseEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Cleanse/CleanseEffect.prefab").WaitForCompletion();
        }

        public override void OnStart()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList.ToList())
            {
                if (!body)
                    continue;

                cleanse(body);
            }
        }

        static void cleanse(CharacterBody body)
        {
            // You can never be *too* safe, my often unreasonable phobia of NullRefs at it again!
            if (_cleanseEffectPrefab && body.hurtBoxGroup && body.hurtBoxGroup.hurtBoxes != null)
            {
                EffectData effectData = new EffectData
                {
                    origin = body.corePosition
                };

                effectData.SetHurtBoxReference(body.mainHurtBox);

                EffectManager.SpawnEffect(_cleanseEffectPrefab, effectData, true);
            }

            Util.CleanseBody(body, true, false, true, true, true, true);
        }
    }
}
