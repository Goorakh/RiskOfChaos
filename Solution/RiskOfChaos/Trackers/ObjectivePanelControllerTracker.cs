using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class ObjectivePanelControllerTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.UI.ObjectivePanelController.SetCurrentMaster += ObjectivePanelController_SetCurrentMaster;
        }

        static void ObjectivePanelController_SetCurrentMaster(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<ObjectivePanelController>(_ => _.RefreshObjectiveTrackers()))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(trackInstance);
                static void trackInstance(ObjectivePanelController objectivePanelController)
                {
                    ObjectivePanelControllerTracker tracker = objectivePanelController.gameObject.EnsureComponent<ObjectivePanelControllerTracker>();
                    tracker.ObjectivePanelController = objectivePanelController;
                }
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }

        public ObjectivePanelController ObjectivePanelController { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
        }

        void onCurrentLanguageChanged()
        {
            foreach (ObjectivePanelController.ObjectiveTracker objectiveTracker in ObjectivePanelController.objectiveTrackers)
            {
                objectiveTracker.cachedString = objectiveTracker.GenerateString();
                objectiveTracker.UpdateStrip();
            }
        }
    }
}
