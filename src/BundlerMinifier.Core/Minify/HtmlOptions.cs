using Newtonsoft.Json.Linq;
using NUglify.Html;

namespace BundlerMinifier
{
    static class HtmlOptions
    {
        public static HtmlSettings GetSettings(Bundle bundle)
        {
            var settings = new HtmlSettings();
            settings.RemoveOptionalTags = GetValue(bundle, "removeOptionalEndTags") == "True";
            //settings.RemoveRedundantAttributes = GetValue(bundle, "removeRedundantAttributes") == "True";

            settings.ShortBooleanAttribute = GetValue(bundle, "collapseBooleanAttributes", true) == "True";
            //settings.CustomAngularDirectiveList = GetValue(bundle, "customAngularDirectiveList");
            //settings.MinifyAngularBindingExpressions = GetValue(bundle, "minifyAngularBindingExpressions") == "True";
            settings.MinifyCss = GetValue(bundle, "minifyEmbeddedCssCode", true) == "True";
            settings.MinifyJs = GetValue(bundle, "minifyEmbeddedJsCode", true) == "True";
            settings.MinifyCssAttributes = GetValue(bundle, "minifyInlineCssCode", true) == "True";
            //settings.MinifyInlineJsCode = GetValue(bundle, "minifyInlineJsCode", true) == "True";
            //settings.MinifyKnockoutBindingExpressions = GetValue(bundle, "minifyKnockoutBindingExpressions") == "True";
            settings.AttributesCaseSensitive = GetValue(bundle, "preserveCase") == "True";
            //settings.ProcessableScriptTypeList = GetValue(bundle, "processableScriptTypeList");
            settings.RemoveComments = GetValue(bundle, "removeHtmlComments", true) == "True";
            //settings.RemoveTagsWithoutContent = GetValue(bundle, "removeTagsWithoutContent") == "True";

            settings.RemoveQuotedAttributes = GetValue(bundle, "attributeQuotesRemovalMode", "html5") != "keepQuotes";
            settings.CollapseWhitespaces = GetValue(bundle, "whitespaceMinificationMode", "medium") != "none";
            settings.IsFragmentOnly = true;

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
