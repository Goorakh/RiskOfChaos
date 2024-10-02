using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities
{
    public static class PopupAlertQueue
    {
        public delegate void SetupDialogDelegate(SimpleDialogBox dialogBox);

        readonly record struct Alert(SetupDialogDelegate SetupDialog);

        [SystemInitializer]
        static void Init()
        {
            static void BaseMainMenuScreen_OnEnter(On.RoR2.UI.MainMenu.BaseMainMenuScreen.orig_OnEnter orig, BaseMainMenuScreen self, MainMenuController mainMenuController)
            {
                orig(self, mainMenuController);

                if (self == mainMenuController.titleMenuScreen)
                {
                    On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter -= BaseMainMenuScreen_OnEnter;
                    startAlertQueue();
                }
            }

            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += BaseMainMenuScreen_OnEnter;
        }

        static readonly Queue<Alert> _alertQueue = [];
        static SimpleDialogBox _currentDialogBox;

        public static void EnqueueAlert(SetupDialogDelegate setupDialog)
        {
            if (setupDialog is null)
                throw new ArgumentNullException(nameof(setupDialog));

            _alertQueue.Enqueue(new Alert(setupDialog));
        }

        static void startAlertQueue()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;
        }

        static void fixedUpdate()
        {
            if (_currentDialogBox || _alertQueue.Count == 0)
                return;

            Alert alert = _alertQueue.Dequeue();

            SimpleDialogBox dialogBox = SimpleDialogBox.Create();
            alert.SetupDialog(dialogBox);

            _currentDialogBox = dialogBox;
        }
    }
}
