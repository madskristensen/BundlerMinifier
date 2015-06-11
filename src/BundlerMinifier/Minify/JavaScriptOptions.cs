using Microsoft.Ajax.Utilities;

namespace BundlerMinifier
{
    class JavaScriptOptions
    {
        public static CodeSettings GetSettings(Bundle bundle)
        {
            CodeSettings settings = new CodeSettings();

            settings.PreserveImportantComments = GetValue(bundle, "preserveImportantComments") == "True";
            settings.TermSemicolons = GetValue(bundle, "termSemicolons") == "True";

            string evalTreatment = GetValue(bundle, "evanTreatment");

            if (evalTreatment == "ignore")
                settings.EvalTreatment = EvalTreatment.Ignore;
            else if (evalTreatment == "makeAllSafe")
                settings.EvalTreatment = EvalTreatment.MakeAllSafe;
            else if (evalTreatment == "makeImmediateSafe")
                settings.EvalTreatment = EvalTreatment.MakeImmediateSafe;

            return settings;
        }

        internal static string GetValue(Bundle bundle, string key)
        {
            if (bundle.Minify.ContainsKey(key))
                return bundle.Minify[key].ToString();

            return string.Empty;
        }
    }
}
