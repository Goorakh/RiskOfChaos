using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class CharacterBodyUtils
    {
        public static void MarkAllBodyStatsDirty()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }
    }
}
