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

            string outputMode = GetValue(bundle, "outputMode");

            if (outputMode == "multipleLines")
                settings.OutputMode = OutputMode.MultipleLines;
            else if (outputMode == "singleLine")
                settings.OutputMode = OutputMode.SingleLine;
            else if (outputMode == "none")
                settings.OutputMode = OutputMode.None;

            string indentSize = GetValue(bundle, "indentSize");
            int size;
            if (int.TryParse(indentSize, out size))
                settings.IndentSize = size;

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
