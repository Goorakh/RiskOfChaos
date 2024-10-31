using RoR2;
using UnityEngine;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class ProjectileIndexConverter : CatalogValueConverter<int>
    {
        public ProjectileIndexConverter() : base(-1)
        {
        }

        protected override int findFromCatalog(string catalogName)
        {
            return ProjectileCatalog.FindProjectileIndex(catalogName);
        }

        protected override string getCatalogName(int value)
        {
            GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(value);
            return projectilePrefab ? projectilePrefab.name : string.Empty;
        }
    }
}
