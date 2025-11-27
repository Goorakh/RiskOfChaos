using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(EffectComponent))]
    public sealed class EquipmentTakenOrbEffect : MonoBehaviour
    {
        public TrailRenderer TrailToColor;

        public ParticleSystem[] ParticlesToColor;

        public SpriteRenderer[] SpritesToColor;

        public SpriteRenderer IconSpriteRenderer;

        void Start()
        {
            EffectComponent effectComponent = GetComponent<EffectComponent>();

            EquipmentIndex equipmentIndex = (EquipmentIndex)Util.UintToIntMinusOne(effectComponent.effectData.genericUInt);

            EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);

            ColorCatalog.ColorIndex colorIndex;
            Sprite sprite;
            if (equipmentDef)
            {
                colorIndex = equipmentDef.colorIndex;
                sprite = equipmentDef.pickupIconSprite;
            }
            else
            {
                colorIndex = ColorCatalog.ColorIndex.Error;
                sprite = null;
            }

            Color color = ColorCatalog.GetColor(colorIndex);

            TrailToColor.startColor *= color;
            TrailToColor.endColor *= color;

            for (int i = 0; i < ParticlesToColor.Length; i++)
            {
                ParticleSystem particleSystem = ParticlesToColor[i];

                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = color;

                particleSystem.Play();
            }

            for (int j = 0; j < SpritesToColor.Length; j++)
            {
                SpritesToColor[j].color = color;
            }

            IconSpriteRenderer.sprite = sprite;
        }
    }
}
