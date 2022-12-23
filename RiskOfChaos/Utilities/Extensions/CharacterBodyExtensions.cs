using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class CharacterBodyExtensions
    {
        public static Quaternion GetRotation(this CharacterBody body)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            return body.transform.rotation;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
