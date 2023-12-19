using RoR2;
using System;
using System.Linq;
using System.Reflection;

namespace RiskOfChaos.ChatMessages
{
    public static class CustomChatMessageHandler
    {
        [SystemInitializer]
        static void Init()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ChatMessageBase))))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                ChatMessageBase.chatMessageTypeToIndex.Add(type, (byte)ChatMessageBase.chatMessageIndexToType.Count);
                ChatMessageBase.chatMessageIndexToType.Add(type);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
