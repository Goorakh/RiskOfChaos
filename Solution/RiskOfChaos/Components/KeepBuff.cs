using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        [SerializeField]
        BuffIndex _buffIndex = BuffIndex.None;
        public BuffIndex BuffIndex
        {
            get
            {
                return _buffIndex;
            }
            set
            {
                if (_buffIndex == value)
                    return;

                _buffIndex = value;

                markBuffsDirty();
            }
        }

        [SerializeField]
        int _minBuffCount = -1;
        public int MinBuffCount
        {
            get
            {
                return _minBuffCount;
            }
            set
            {
                if (_minBuffCount == value)
                    return;

                _minBuffCount = value;
                markBuffsDirty();
            }
        }

        CharacterBody _body;

        BuffIndex _appliedBuffIndex = BuffIndex.None;
        int _appliedBuffCount;

        bool _buffsDirty;

        void Awake()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Activated on client");
                enabled = false;
                return;
            }

            _body = GetComponent<CharacterBody>();
        }

        void OnEnable()
        {
            refreshBuffs();
        }

        void OnDisable()
        {
            removeAppliedBuffs();
        }

        void markBuffsDirty()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_buffsDirty)
                return;

            _buffsDirty = true;

            RoR2Application.onNextUpdate += refreshBuffs;
        }

        void refreshBuffs()
        {
            _buffsDirty = false;

            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            removeAppliedBuffs();

            _appliedBuffIndex = _buffIndex;
            _appliedBuffCount = 0;

            if (_appliedBuffIndex != BuffIndex.None)
            {
                // Kinda ugly, but it does work, all that's needed is a call to SetBuffCount for the right buff
                int oldBuffCount = _body.GetBuffCount(_appliedBuffIndex);

                _body.SetBuffCount(_appliedBuffIndex, oldBuffCount);
                _appliedBuffCount = _body.GetBuffCount(_appliedBuffIndex) - oldBuffCount;
            }
        }

        void removeAppliedBuffs()
        {
            if (_appliedBuffIndex != BuffIndex.None && _appliedBuffCount != 0)
            {
                _body.SetBuffCount(_appliedBuffIndex, _body.GetBuffCount(_appliedBuffIndex) - _appliedBuffCount);
            }

            _appliedBuffIndex = BuffIndex.None;
            _appliedBuffCount = 0;
        }
    }
}
