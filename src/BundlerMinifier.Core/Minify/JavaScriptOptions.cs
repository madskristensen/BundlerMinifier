﻿using NUglify;
using NUglify.JavaScript;

namespace BundlerMinifier
{
    static class JavaScriptOptions
    {
        public static CodeSettings GetSettings(Bundle bundle)
        {
            CodeSettings settings = new CodeSettings();
            settings.AlwaysEscapeNonAscii = GetValue(bundle, "alwaysEscapeNonAscii", false) == "True";

            settings.PreserveImportantComments = GetValue(bundle, "preserveImportantComments", true) == "True";
            settings.TermSemicolons = GetValue(bundle, "termSemicolons", true) == "True";

            if (GetValue(bundle, "renameLocals", true) == "False")
                settings.LocalRenaming = LocalRenaming.KeepAll;

            string evalTreatment = GetValue(bundle, "evalTreatment", "ignore");

            if (evalTreatment == "ignore")
                settings.EvalTreatment = EvalTreatment.Ignore;
            else if (evalTreatment == "makeAllSafe")
                settings.EvalTreatment = EvalTreatment.MakeAllSafe;
            else if (evalTreatment == "makeImmediateSafe")
                settings.EvalTreatment = EvalTreatment.MakeImmediateSafe;

            string outputMode = GetValue(bundle, "outputMode", "singleLine");

            if (outputMode == "multipleLines")
                settings.OutputMode = OutputMode.MultipleLines;
            else if (outputMode == "singleLine")
                settings.OutputMode = OutputMode.SingleLine;
            else if (outputMode == "none")
                settings.OutputMode = OutputMode.None;

            string indentSize = GetValue(bundle, "indentSize", 2);
            int size;
            if (int.TryParse(indentSize, out size))
                settings.IndentSize = size;

            settings.IgnoreErrorList = GetValue(bundle, "ignoreErrorList", "");

            return settings;
        }

        internal static string GetValue(Bundle bundle, string key, object defaultValue = null)
        {
            if (bundle.Minify.ContainsKey(key))
                return bundle.Minify[key].ToString();

            if (defaultValue != null)
                return defaultValue.ToString();

            return string.Empty;
        }
    }
}
