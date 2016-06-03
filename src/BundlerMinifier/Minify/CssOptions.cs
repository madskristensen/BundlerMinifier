using NUglify;
using NUglify.Css;

namespace BundlerMinifier
{
    static class CssOptions
    {
        public static CssSettings GetSettings(Bundle bundle)
        {
            CssSettings settings = new CssSettings();
            settings.TermSemicolons = GetValue(bundle, "termSemicolons") == "True";

            string cssComment = GetValue(bundle, "commentMode");

            if (cssComment == "hacks")
                settings.CommentMode = CssComment.Hacks;
            else if (cssComment == "important")
                settings.CommentMode = CssComment.Important;
            else if (cssComment == "none")
                settings.CommentMode = CssComment.None;
            else if (cssComment == "all")
                settings.CommentMode = CssComment.All;

            string colorNames = GetValue(bundle, "colorNames");

            if (colorNames == "hex")
                settings.ColorNames = CssColor.Hex;
            else if (colorNames == "major")
                settings.ColorNames = CssColor.Major;
            else if (colorNames == "noSwap")
                settings.ColorNames = CssColor.NoSwap;
            else if (colorNames == "strict")
                settings.ColorNames = CssColor.Strict;

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
