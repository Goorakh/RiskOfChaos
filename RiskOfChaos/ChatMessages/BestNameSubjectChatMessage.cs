using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ChatMessages
{
    public class BestNameSubjectChatMessage : ChatMessageBase
    {
        GameObject _subjectNetworkUserObject;
        MemoizedGetComponent<NetworkUser> _subjectNetworkUserGetComponent;

        GameObject _subjectCharacterBodyGameObject;
        MemoizedGetComponent<CharacterBody> _subjectCharacterBodyGetComponent;

        public string BaseToken;

        public NetworkUser SubjectAsNetworkUser
        {
            get
            {
                return _subjectNetworkUserGetComponent.Get(_subjectNetworkUserObject);
            }
            set
            {
                _subjectNetworkUserObject = value ? value.gameObject : null;

                CharacterBody characterBody = value ? value.GetCurrentBody() : null;
                _subjectCharacterBodyGameObject = characterBody ? characterBody.gameObject : null;
            }
        }

        public CharacterBody SubjectAsCharacterBody
        {
            get
            {
                return _subjectCharacterBodyGetComponent.Get(_subjectCharacterBodyGameObject);
            }
            set
            {
                _subjectCharacterBodyGameObject = value ? value.gameObject : null;

                NetworkUser networkUser = Util.LookUpBodyNetworkUser(value);
                _subjectNetworkUserObject = networkUser ? networkUser.gameObject : null;
            }
        }

        protected string getSubjectName()
        {
            if (SubjectAsNetworkUser)
            {
                return Util.EscapeRichTextForTextMeshPro(SubjectAsNetworkUser.userName);
            }

            if (SubjectAsCharacterBody)
            {
                return Util.GetBestBodyName(SubjectAsCharacterBody.gameObject);
            }

            return "???";
        }

        protected bool isSecondPerson()
        {
            return LocalUserManager.readOnlyLocalUsersList.Count == 1 && SubjectAsNetworkUser && SubjectAsNetworkUser.localUser != null;
        }

        protected string getResolvedToken()
        {
            if (isSecondPerson())
            {
                string secondPersonToken = BaseToken + "_2P";
                if (!Language.IsTokenInvalid(secondPersonToken))
                {
                    return secondPersonToken;
                }
            }

            return BaseToken;
        }

        public override string ConstructChatString()
        {
            return Language.GetStringFormatted(getResolvedToken(), getSubjectName());
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_subjectNetworkUserObject);
            writer.Write(_subjectCharacterBodyGameObject);
            writer.Write(BaseToken);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _subjectNetworkUserObject = reader.ReadGameObject();
            _subjectCharacterBodyGameObject = reader.ReadGameObject();
            BaseToken = reader.ReadString();
        }
    }
}
