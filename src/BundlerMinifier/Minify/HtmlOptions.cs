using WebMarkupMin.Core;
using WebMarkupMin.Core.Settings;

namespace BundlerMinifier
{
    class HtmlOptions
    {
        public static HtmlMinificationSettings GetSettings(Bundle bundle)
        {
            HtmlMinificationSettings settings = new HtmlMinificationSettings();
            settings.RemoveOptionalEndTags = GetValue(bundle, "removeOptionalEndTags") == "true";
            settings.RemoveRedundantAttributes = GetValue(bundle, "removeRedundantAttributes") == "true";

            string quotes = GetValue(bundle, "attributeQuotesRemovalMode");

            if (quotes == "html4")
                settings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.Html4;
            else if (quotes == "html5")
                settings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.Html5;
            else if (quotes == "keepQuotes")
                settings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes;

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
