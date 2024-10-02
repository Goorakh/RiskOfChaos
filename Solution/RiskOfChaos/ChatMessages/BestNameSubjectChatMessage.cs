using RoR2;
using UnityEngine;

namespace RiskOfChaos.ChatMessages
{
    public class BestNameSubjectChatMessage : ChatMessageBase
    {
        public GameObject SubjectNetworkUserObject;
        MemoizedGetComponent<NetworkUser> _subjectNetworkUserGetComponent;

        public GameObject SubjectCharacterBodyObject;
        MemoizedGetComponent<CharacterBody> _subjectCharacterBodyGetComponent;

        public string BaseToken;

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
    }
}
