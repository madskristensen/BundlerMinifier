using Microsoft.Ajax.Utilities;

namespace BundlerMinifier
{
    class CssOptions
    {
        public static CssSettings GetSettings(Bundle bundle)
        {
            CssSettings settings = new CssSettings();
            settings.TermSemicolons = GetValue(bundle, "termSemicolons") == "true";

            string cssComment = GetValue(bundle, "cssComment");

            if (cssComment == "hacks")
                settings.CommentMode = CssComment.Hacks;
            else if (cssComment == "important")
                settings.CommentMode = CssComment.Important;
            else if (cssComment == "none")
                settings.CommentMode = CssComment.None;

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
