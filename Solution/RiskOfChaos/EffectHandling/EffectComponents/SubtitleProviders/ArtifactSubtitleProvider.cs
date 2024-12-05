using RiskOfChaos.Content;
using RoR2;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    [RequiredComponents(typeof(ChaosEffectSubtitleComponent))]
    public class ArtifactSubtitleProvider : NetworkBehaviour, IEffectSubtitleProvider
    {
        [SyncVar(hook = nameof(hookSetArtifactIndex))]
        int _artifactIndexInternal;

        public ArtifactIndex ArtifactIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ArtifactIndex)(_artifactIndexInternal - 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _artifactIndexInternal = (int)value + 1;
        }

        string _cachedSubtitle;

        public event Action<IEffectSubtitleProvider> OnSubtitleChanged;

        public override void OnStartClient()
        {
            base.OnStartClient();
            markSubtitleDirty();
        }

        void OnEnable()
        {
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
            markSubtitleDirty();
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
        }

        public string GetSubtitle()
        {
            _cachedSubtitle ??= generateSubtitle();
            return _cachedSubtitle;
        }

        string generateSubtitle()
        {
            ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(ArtifactIndex);
            if (!artifactDef)
                return string.Empty;

            return $"(<style=cArtifact>{Language.GetString(artifactDef.nameToken)}</style>)";
        }

        void onCurrentLanguageChanged()
        {
            markSubtitleDirty();
        }

        void markSubtitleDirty()
        {
            _cachedSubtitle = null;
            OnSubtitleChanged?.Invoke(this);
        }

        void hookSetArtifactIndex(int artifactIndexInt)
        {
            _artifactIndexInternal = artifactIndexInt;
            markSubtitleDirty();
        }
    }
}
