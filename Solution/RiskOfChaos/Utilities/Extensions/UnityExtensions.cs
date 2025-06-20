﻿using HG;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class UnityExtensions
    {
        public static void SetDontDestroyOnLoad(this GameObject obj, bool dontDestroyOnLoad)
        {
            if (dontDestroyOnLoad)
            {
                obj.EnsureComponent<SetDontDestroyOnLoad>();
            }
            else
            {
                if (obj.TryGetComponent(out SetDontDestroyOnLoad setDontDestroyOnLoad))
                {
                    GameObject.Destroy(setDontDestroyOnLoad);
                }

                if (Util.IsDontDestroyOnLoad(obj))
                {
                    SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
                }
            }

            if (NetworkServer.active && !NetworkServer.dontListen && obj.GetComponent<NetworkIdentity>())
            {
                Log.Debug($"Syncing DontDesroyOnLoad state for {obj}: {dontDestroyOnLoad}");

                new SetObjectDontDestroyOnLoadMessage(obj, dontDestroyOnLoad).Send(NetworkDestination.Clients);
            }
        }

        public static Component EnsureComponent(this GameObject obj, Type componentType)
        {
            if (!obj.TryGetComponent(componentType, out Component component))
            {
                component = obj.AddComponent(componentType);
            }

            return component;
        }
    }
}
