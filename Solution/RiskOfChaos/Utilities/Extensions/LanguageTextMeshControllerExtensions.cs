using RoR2.UI;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class LanguageTextMeshControllerExtensions
    {
        // token and formatArgs setters immediately attempt to resolve the string before the other setter can be updated, and this can cause format exceptions
        public static void SetTokenAndFormatArgs(this LanguageTextMeshController languageTextMeshController, string token, object[] formatArgs)
        {
            languageTextMeshController._token = token;
            languageTextMeshController._formatArgs = formatArgs ?? [];

            languageTextMeshController.ResolveString();
            languageTextMeshController.UpdateLabel();
        }
    }
}
