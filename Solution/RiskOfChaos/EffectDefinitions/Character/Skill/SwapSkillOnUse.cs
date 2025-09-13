using EntityStates.Toolbot;
using HG;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Collections;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Networking;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Skill
{
    /*
    [ChaosTimedEffect("swap_skill_on_use", 90f)]
    public sealed class SwapSkillOnUse : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _skillSwapInterval =
            ConfigFactory<int>.CreateConfig("Activations Per Skill", 1)
                              .Description("How many times any given skill should be activated before swapping to another.")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        readonly ClearingObjectList<SwapSkillOnUseController> _skillSwapControllers = [];

        readonly Dictionary<int, ReadOnlyArray<SkillFamily.Variant>> _skillFamilyVariantOrder = [];

        ChaosEffectComponent _effectComponent;

        [SyncVar]
        [SerializedMember("rng")]
        ulong _rngSeed;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rngSeed = _effectComponent.Rng.nextUlong;
        }
        
        void Start()
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(_rngSeed);

            foreach (SkillFamily skillFamily in SkillCatalog.allSkillFamilies)
            {
                if (skillFamily.variants.Length > 1 && !_skillFamilyVariantOrder.ContainsKey(skillFamily.catalogIndex))
                {
                    SkillFamily.Variant[] variants = ArrayUtils.Clone(skillFamily.variants);
                    Util.ShuffleArray(variants, rng);

                    _skillFamilyVariantOrder[skillFamily.catalogIndex] = variants;
                }
            }

            _skillSwapControllers.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                tryAddComponentToBody(body);
            }

            CharacterBody.onBodyStartGlobal += tryAddComponentToBody;
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= tryAddComponentToBody;

            _skillSwapControllers.ClearAndDispose(true);
        }

        void tryAddComponentToBody(CharacterBody body)
        {
            if (body.hasEffectiveAuthority)
            {
                SwapSkillOnUseController skillSwapController = body.gameObject.AddComponent<SwapSkillOnUseController>();
                skillSwapController.EffectController = this;
                _skillSwapControllers.Add(skillSwapController);
            }
        }

        public SkillDef GetNextSkill(SkillDef currentSkill, SkillFamily skillFamily, CharacterBody ownerBody)
        {
            if (!skillFamily)
                return currentSkill;

            if (!_skillFamilyVariantOrder.TryGetValue(skillFamily.catalogIndex, out ReadOnlyArray<SkillFamily.Variant> skillVariantOrder))
                return currentSkill;

            int currentVariantIndex = -1;

            for (int i = 0; i < skillVariantOrder.Length; i++)
            {
                if (skillVariantOrder[i].skillDef == currentSkill)
                {
                    currentVariantIndex = i;
                    break;
                }
            }

            if (currentVariantIndex == -1)
            {
                currentVariantIndex = ClampedConversion.Int32(skillFamily.defaultVariantIndex) - 1;
            }

            NetworkUser bodyNetworkUser = Util.LookUpBodyNetworkUser(ownerBody);

            LocalUser bodyLocalUser = bodyNetworkUser ? bodyNetworkUser.localUser : null;

            SkillDef nextSkill = currentSkill;

            for (int variantOffset = 1; variantOffset < skillVariantOrder.Length; variantOffset++)
            {
                int variantIndex = (currentVariantIndex + variantOffset) % skillVariantOrder.Length;
                SkillFamily.Variant skillVariant = skillVariantOrder[variantIndex];

                bool isVariantLocked = false;
                if (skillVariant.unlockableDef)
                {
                    if (bodyLocalUser != null && bodyLocalUser.userProfile != null)
                    {
                        isVariantLocked = !bodyLocalUser.userProfile.HasUnlockable(skillVariant.unlockableDef);
                    }
                    else if (bodyNetworkUser)
                    {
                        isVariantLocked = !bodyNetworkUser.unlockables.Contains(skillVariant.unlockableDef);
                    }
                }

                if (!isVariantLocked)
                {
                    nextSkill = skillVariant.skillDef;
                    break;
                }
            }

            return nextSkill;
        }

        class SwapSkillOnUseController : MonoBehaviour
        {
            public SwapSkillOnUse EffectController;

            CharacterBody _body;

            struct SkillActivationInfo
            {
                public readonly GenericSkill Skill;

                public int ActivationCounter;

                public EntityStateMachine MostRecentSkillStateMachine;

                public SkillActivationInfo(GenericSkill skill)
                {
                    Skill = skill;
                    ActivationCounter = 0;
                    MostRecentSkillStateMachine = skill.stateMachine;
                }
            }

            SkillActivationInfo[] _skillActivationInfos;

            bool _loadoutDirty;
            float _loadoutRefreshTimer;

            void Awake()
            {
                _body = GetComponent<CharacterBody>();

                _skillActivationInfos = new SkillActivationInfo[_body.skillLocator.skillSlotCount];
                for (int i = 0; i < _skillActivationInfos.Length; i++)
                {
                    _skillActivationInfos[i] = new SkillActivationInfo(_body.skillLocator.GetSkillAtIndex(i));
                }
            }

            void OnEnable()
            {
                _body.onSkillActivatedAuthority += onSkillActivatedAuthority;
            }

            void OnDisable()
            {
                _body.onSkillActivatedAuthority -= onSkillActivatedAuthority;
            }

            void onSkillActivatedAuthority(GenericSkill skill)
            {
                GenericSkill activatedSkill = skill;
                EntityStateMachine activatedStateMachine = activatedSkill.stateMachine;
                if (activatedSkill.currentSkillOverride != -1)
                {
                    GenericSkill overrideSkillSource = null;

                    GenericSkill.SkillOverride activeSkillOverride = ArrayUtils.GetSafe(activatedSkill.skillOverrides, activatedSkill.currentSkillOverride);
                    if (activeSkillOverride != null)
                    {
                        if (activeSkillOverride.source is ToolbotDualWieldBase toolbotDualWield &&
                            activatedSkill == _body.skillLocator.secondary)
                        {
                            overrideSkillSource = toolbotDualWield.primary2Slot;
                        }
                    }

                    if (!overrideSkillSource)
                    {
                        Log.Debug($"Not counting use of overriden skill {activatedSkill.skillDef} (index={_body.skillLocator.GetSkillSlotIndex(activatedSkill)}) for {FormatUtils.GetBestBodyName(_body)}");
                        return;
                    }

                    Log.Debug($"Determined override skill source {overrideSkillSource.skillDef} (index={_body.skillLocator.GetSkillSlotIndex(overrideSkillSource)}) for {FormatUtils.GetBestBodyName(_body)}");

                    activatedSkill = overrideSkillSource;
                }

                int activatedSkillSlotIndex = _body.skillLocator.GetSkillSlotIndex(activatedSkill);

                if (!ArrayUtils.IsInBounds(_skillActivationInfos, activatedSkillSlotIndex))
                {
                    Log.Error($"Skill {activatedSkill.skillName} not found in {FormatUtils.GetBestBodyName(_body)} skill locator");
                    return;
                }

                ref SkillActivationInfo skillActivationInfo = ref _skillActivationInfos[activatedSkillSlotIndex];
                skillActivationInfo.ActivationCounter++;
                skillActivationInfo.MostRecentSkillStateMachine = activatedStateMachine;
            }

            void FixedUpdate()
            {
                _loadoutRefreshTimer -= Time.fixedDeltaTime;
                if (_loadoutRefreshTimer <= 0f)
                {
                    _loadoutRefreshTimer = 0.5f;

                    if (_loadoutDirty)
                    {
                        _loadoutDirty = false;

                        if (_body.master)
                        {
                            Loadout loadout = new Loadout();
                            _body.master.loadout.Copy(loadout);

                            for (int i = 0; i < _skillActivationInfos.Length; i++)
                            {
                                GenericSkill skill = _skillActivationInfos[i].Skill;

                                uint skillVariantIndex = 0;

                                if (skill.skillFamily)
                                {
                                    for (uint j = 0; j < skill.skillFamily.variants.Length; j++)
                                    {
                                        if (skill.skillFamily.variants[j].skillDef == skill.baseSkill)
                                        {
                                            skillVariantIndex = j;
                                            break;
                                        }
                                    }
                                }

                                loadout.bodyLoadoutManager.SetSkillVariant(_body.bodyIndex, i, skillVariantIndex);
                            }

                            Log.Debug($"Sending loadout update for {FormatUtils.GetBestBodyName(_body)}");

                            new SetMasterLoadoutMessage(_body.masterObject, loadout).Send(NetworkDestination.Server);
                        }
                    }
                }

                for (int i = 0; i < _skillActivationInfos.Length; i++)
                {
                    ref SkillActivationInfo skillActivationInfo = ref _skillActivationInfos[i];

                    GenericSkill skill = skillActivationInfo.Skill;
                    if (!skill)
                        continue;

                    if (skillActivationInfo.ActivationCounter >= _skillSwapInterval.Value)
                    {
                        EntityStateMachine skillStateMachine = skill.stateMachine;
                        if (skillActivationInfo.MostRecentSkillStateMachine)
                        {
                            skillStateMachine = skillActivationInfo.MostRecentSkillStateMachine;
                        }

                        if (skillStateMachine && skillStateMachine.state.GetType() != skill.activationState.stateType)
                        {
                            SkillDef nextBaseSkill = null;
                            if (EffectController)
                            {
                                nextBaseSkill = EffectController.GetNextSkill(skill.baseSkill, skill.skillFamily, _body);
                            }

                            if (nextBaseSkill && skill.baseSkill != nextBaseSkill)
                            {
                                Log.Debug($"{FormatUtils.GetBestBodyName(_body)}: changing base skill {skill} (index {i}) to {nextBaseSkill}");

                                float oldStockFraction = 1f;
                                if (skill.maxStock > 0)
                                {
                                    oldStockFraction = skill.stock / (float)skill.maxStock;
                                }

                                skill.SetBaseSkill(nextBaseSkill);

                                skill.stock = Mathf.Min(skill.stock, Mathf.CeilToInt(oldStockFraction * skill.maxStock));

                                markLoadoutDirty();
                            }

                            skillActivationInfo.ActivationCounter = 0;
                        }
                    }
                }
            }

            void markLoadoutDirty()
            {
                _loadoutDirty = true;
            }
        }
    }
    */
}
