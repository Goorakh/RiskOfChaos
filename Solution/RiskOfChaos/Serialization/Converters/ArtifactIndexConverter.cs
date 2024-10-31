using RoR2;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class ArtifactIndexConverter : CatalogValueConverter<ArtifactIndex>
    {
        public ArtifactIndexConverter() : base(ArtifactIndex.None)
        {
        }

        protected override ArtifactIndex findFromCatalog(string catalogName)
        {
            return ArtifactCatalog.FindArtifactIndex(catalogName);
        }

        protected override string getCatalogName(ArtifactIndex value)
        {
            ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(value);
            return artifactDef ? artifactDef.cachedName : string.Empty;
        }
    }
}
