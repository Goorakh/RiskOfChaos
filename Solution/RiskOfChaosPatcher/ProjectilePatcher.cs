using BepInEx.Logging;
using Mono.Cecil;
using System.Collections.Generic;

namespace RiskOfChaosPatcher
{
    public class ProjectilePatcher
    {
        static readonly LogWriter _log = new LogWriter();

        public static IEnumerable<string> TargetDLLs { get; } = [AssemblyNames.RoR2];

        public static void Initialize()
        {
            _log.SetLogSource(Logger.CreateLogSource(nameof(ProjectilePatcher)));
        }

        public static void Patch(AssemblyDefinition assembly)
        {
            TypeDefinition fireProjectileInfoType = assembly.MainModule.GetType("RoR2.Projectile.FireProjectileInfo");
            if (fireProjectileInfoType == null)
            {
                _log.Error("Failed to find type: FireProjectileInfo");
                return;
            }

            TypeReference floatTypeRef = assembly.MainModule.ImportReference(typeof(float));

            addField(fireProjectileInfoType, new FieldDefinition("roc_procCoefficientOverridePlusOne", FieldAttributes.Public, floatTypeRef));
        }

        static void addField(TypeDefinition declaringType, FieldDefinition field)
        {
            declaringType.Fields.Add(field);
            _log.Debug($"Added field {field.Attributes} {field.FieldType.FullName} {field.Name} to {declaringType.FullName}");
        }
    }
}
