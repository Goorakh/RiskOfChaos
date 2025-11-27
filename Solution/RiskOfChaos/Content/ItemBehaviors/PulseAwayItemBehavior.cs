using HG;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.Content.ItemBehaviors
{
    public sealed class PulseAwayItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        static ItemDef GetItemDef()
        {
            return RoCContent.Items.PulseAway;
        }

        const float PULSE_INTERVAL = 1f;

        AssetOrDirectReference<GameObject> _pulsePrefabReference;

        IPhysMotor _bodyMotor;

        float _pulseSpawnTimer = 0f;

        void OnEnable()
        {
            _bodyMotor = GetComponent<IPhysMotor>();

            _pulseSpawnTimer = 0f;

            _pulsePrefabReference ??= new AssetOrDirectReference<GameObject>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = new AssetReferenceGameObject(AddressableGuids.RoR2_Base_moon2_MoonBatteryDesignPulse_prefab)
            };
        }

        void OnDestroy()
        {
            _pulsePrefabReference?.Reset();
            _pulsePrefabReference = null;
        }

        void FixedUpdate()
        {
            _pulseSpawnTimer += Time.fixedDeltaTime;
            if (_pulseSpawnTimer >= PULSE_INTERVAL)
            {
                _pulseSpawnTimer = 0f;

                if (body && body.healthComponent && body.healthComponent.alive)
                {
                    spawnPulse();
                }
            }
        }

        void spawnPulse()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            GameObject pulseControllerObj = Instantiate(_pulsePrefabReference.WaitForCompletion(), body.footPosition, Quaternion.identity);
            if (!pulseControllerObj)
                return;

            SphereSearch pulseSearch = new SphereSearch();
            PulseController pulseController = pulseControllerObj.GetComponent<PulseController>();
            pulseController.performSearch += performPulseSearch;
            pulseController.onPulseHit += onPulseHit;

            pulseController.NetworkfinalRadius = 10f + (body.radius * 5f);

            void performPulseSearch(PulseController pulseController, Vector3 origin, float radius, List<PulseController.PulseSearchResult> dest)
            {
                TeamMask teamMask = TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex);

                pulseSearch.origin = origin;
                pulseSearch.radius = radius;
                pulseSearch.queryTriggerInteraction = QueryTriggerInteraction.Ignore;
                pulseSearch.mask = LayerIndex.entityPrecise.mask;
                pulseSearch.RefreshCandidates();
                pulseSearch.FilterCandidatesByHurtBoxTeam(teamMask);
                pulseSearch.OrderCandidatesByDistance();
                pulseSearch.FilterCandidatesByDistinctHurtBoxEntities();

                using (ListPool<HurtBox>.RentCollection(out List<HurtBox> hurtBoxes))
                {
                    pulseSearch.GetHurtBoxes(hurtBoxes);

                    foreach (HurtBox hurtBox in hurtBoxes)
                    {
                        if (hurtBox && hurtBox.healthComponent && hurtBox.healthComponent.alive && hurtBox.healthComponent.body != body)
                        {
                            Vector3 closestPoint = hurtBox.collider.ClosestPoint(pulseSearch.origin);

                            dest.Add(new PulseController.PulseSearchResult
                            {
                                hitObject = hurtBox.healthComponent,
                                hitPos = closestPoint
                            });
                        }
                    }
                }
            }

            void onPulseHit(PulseController pulseController, PulseController.PulseHit hitInfo)
            {
                HealthComponent hitHealthComponent = hitInfo.hitObject as HealthComponent;
                if (!hitHealthComponent)
                    return;

                CharacterBody hitBody = hitHealthComponent.body;
                if (!hitBody)
                    return;

                IPhysMotor hitMotor = hitBody.GetComponent<IPhysMotor>();
                if (hitMotor == null)
                    return;

                bool grounded = false;
                if (hitBody.characterMotor)
                {
                    grounded = hitBody.characterMotor.isGrounded;
                }
                else
                {
                    grounded = Physics.CheckSphere(hitBody.corePosition, hitBody.bestFitActualRadius + 0.1f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
                }

                if (!grounded)
                    return;

                Vector3 horizontalForceDir = (hitInfo.hitPos - hitInfo.pulseOrigin).normalized;
                horizontalForceDir.y = 0f;
                horizontalForceDir.Normalize();

                float mass = 0f;
                if (_bodyMotor != null)
                {
                    mass = _bodyMotor.mass;
                }

                float baseMaxForce = Mathf.Clamp(mass * 25f, 5000f, 10000f);

                const float VERTICAL_FORCE_FRACTION = 0.175f;

                float baseHorizontalForce = baseMaxForce * (1f - VERTICAL_FORCE_FRACTION);
                float baseVerticalForce = baseMaxForce * VERTICAL_FORCE_FRACTION;

                Vector3 horizontalForce = horizontalForceDir * baseHorizontalForce;
                Vector3 verticalForce = Vector3.up * baseVerticalForce;

                hitMotor.ApplyForceImpulse(new PhysForceInfo
                {
                    force = horizontalForce + verticalForce,
                    ignoreGroundStick = true
                });

                hitBody.AddTimedBuff(RoR2Content.Buffs.Cripple, 2f);
            }

            pulseController.StartPulseServer();

            NetworkServer.Spawn(pulseControllerObj);
        }
    }
}
