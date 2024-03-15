using RoR2.ContentManagement;
using System.Collections;

namespace RiskOfChaos.Content
{
    public class ContentPackProvider : IContentPackProvider
    {
        readonly ContentPack _contentPack = new ContentPack();

        public string identifier => Main.PluginGUID;

        bool _isRegistered;

        internal ContentPackProvider()
        {
        }

        internal void Register()
        {
            if (_isRegistered)
                return;

            ContentManager.collectContentPackProviders += addContentPackProviderDelegate =>
            {
                addContentPackProviderDelegate(this);
            };

            _isRegistered = true;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            _contentPack.identifier = identifier;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            Items.AddItemDefsTo(_contentPack.itemDefs);
            Buffs.AddBuffDefsTo(_contentPack.buffDefs);
            BodyPrefabs.AddBodyPrefabsTo(_contentPack.bodyPrefabs);
            Effects.AddEffectDefsTo(_contentPack.effectDefs);
            EntityStateTypeAttribute.AddStateTypesTo(_contentPack.entityStateTypes);
            Unlockables.AddUnlockablesTo(_contentPack.unlockableDefs);

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(_contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
