using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ChatMessages
{
    public class BestNameSubjectChatMessage : ChatMessageBase
    {
        public GameObject SubjectNetworkUserObject;
        MemoizedGetComponent<NetworkUser> _subjectNetworkUserGetComponent;

        public GameObject SubjectCharacterBodyObject;
        MemoizedGetComponent<CharacterBody> _subjectCharacterBodyGetComponent;

        public string BaseToken;

        public Color32? SubjectNameOverrideColor;

        public NetworkUser SubjectAsNetworkUser
        {
            get
            {
                return _subjectNetworkUserGetComponent.Get(SubjectNetworkUserObject);
            }
            set
            {
                SubjectNetworkUserObject = value ? value.gameObject : null;

                CharacterBody characterBody = value ? value.GetCurrentBody() : null;
                SubjectCharacterBodyObject = characterBody ? characterBody.gameObject : null;
            }
        }

        public CharacterBody SubjectAsCharacterBody
        {
            get
            {
                return _subjectCharacterBodyGetComponent.Get(SubjectCharacterBodyObject);
            }
            set
            {
                SubjectCharacterBodyObject = value ? value.gameObject : null;

                NetworkUser networkUser = Util.LookUpBodyNetworkUser(value);
                SubjectNetworkUserObject = networkUser ? networkUser.gameObject : null;
            }
        }

        protected string getSubjectName()
        {
            string subjectName = "???";
            if (SubjectAsNetworkUser)
            {
                subjectName = Util.EscapeRichTextForTextMeshPro(SubjectAsNetworkUser.userName);
            }
            else if (SubjectAsCharacterBody)
            {
                subjectName = Util.GetBestBodyName(SubjectAsCharacterBody.gameObject);
            }

            if (SubjectNameOverrideColor.HasValue)
            {
                subjectName = Util.GenerateColoredString(subjectName, SubjectNameOverrideColor.Value);
            }

            return subjectName;
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

            writer.Write(SubjectNetworkUserObject);
            writer.Write(SubjectCharacterBodyObject);

            writer.Write(BaseToken);

            writer.Write(SubjectNameOverrideColor.HasValue);
            if (SubjectNameOverrideColor.HasValue)
            {
                writer.Write(SubjectNameOverrideColor.Value);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            SubjectNetworkUserObject = reader.ReadGameObject();
            SubjectCharacterBodyObject = reader.ReadGameObject();

            BaseToken = reader.ReadString();

            SubjectNameOverrideColor = reader.ReadBoolean() ? reader.ReadColor32() : null;
        }
    }
}
