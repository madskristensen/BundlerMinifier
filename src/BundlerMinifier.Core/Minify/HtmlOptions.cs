using Newtonsoft.Json.Linq;
using NUglify.Html;

namespace BundlerMinifier
{
    static class HtmlOptions
    {
        public static HtmlSettings GetSettings(Bundle bundle)
        {
            var settings = new HtmlSettings
            {
                RemoveOptionalTags = GetValue(bundle, "removeOptionalEndTags") == "True",
                ShortBooleanAttribute = GetValue(bundle, "collapseBooleanAttributes", true) == "True",
                MinifyCss = GetValue(bundle, "minifyEmbeddedCssCode", true) == "True",
                MinifyJs = GetValue(bundle, "minifyEmbeddedJsCode", true) == "True",
                MinifyCssAttributes = GetValue(bundle, "minifyInlineCssCode", false) == "True",
                AttributesCaseSensitive = GetValue(bundle, "preserveCase") == "True",
                RemoveComments = GetValue(bundle, "removeHtmlComments", true) == "True",
                RemoveQuotedAttributes = GetValue(bundle, "removeQuotedAttributes", true) == "True",
                CollapseWhitespaces = GetValue(bundle, "collapseWhitespace", true) == "True",
                IsFragmentOnly = GetValue(bundle, "isFragmentOnly", true) == "True"
            };

            return settings;
        }

        internal static string GetValue(Bundle bundle, string key, object defaultValue = null)
        {
            if (bundle.Minify.ContainsKey(key))
            {
                object value = bundle.Minify[key];
                if (value is JArray)
                {
                    return string.Join(",", ((JArray)value).Values<string>());
                }
                else
                {
                    return value.ToString();
                }
            }

            if (defaultValue != null)
                return defaultValue.ToString();

            return string.Empty;
        }
    }
}
