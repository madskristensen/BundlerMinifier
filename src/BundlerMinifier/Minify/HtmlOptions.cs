using Newtonsoft.Json.Linq;
using WebMarkupMin.Core;
using WebMarkupMin.Core.Settings;

namespace BundlerMinifier
{
    static class HtmlOptions
    {
        public static HtmlMinificationSettings GetSettings(Bundle bundle)
        {
            HtmlMinificationSettings settings = new HtmlMinificationSettings();
            settings.RemoveOptionalEndTags = GetValue(bundle, "removeOptionalEndTags") == "True";
            settings.RemoveRedundantAttributes = GetValue(bundle, "removeRedundantAttributes") == "True";

            settings.CollapseBooleanAttributes = GetValue(bundle, "collapseBooleanAttributes", true) == "True";
            settings.CustomAngularDirectiveList = GetValue(bundle, "customAngularDirectiveList");
            settings.MinifyAngularBindingExpressions = GetValue(bundle, "minifyAngularBindingExpressions") == "True";
            settings.MinifyEmbeddedCssCode = GetValue(bundle, "minifyEmbeddedCssCode", true) == "True";
            settings.MinifyEmbeddedJsCode= GetValue(bundle, "minifyEmbeddedJsCode", true) == "True";
            settings.MinifyInlineCssCode = GetValue(bundle, "minifyInlineCssCode", true) == "True";
            settings.MinifyInlineJsCode = GetValue(bundle, "minifyInlineJsCode", true) == "True";
            settings.MinifyKnockoutBindingExpressions = GetValue(bundle, "minifyKnockoutBindingExpressions") == "True";
            settings.ProcessableScriptTypeList = GetValue(bundle, "processableScriptTypeList");
            settings.RemoveHtmlComments = GetValue(bundle, "removeHtmlComments", true) == "True";
            settings.RemoveTagsWithoutContent = GetValue(bundle, "removeTagsWithoutContent") == "True";

            string quotes = GetValue(bundle, "attributeQuotesRemovalMode", "html5");

            if (quotes == "html4")
                settings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.Html4;
            else if (quotes == "html5")
                settings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.Html5;
            else if (quotes == "keepQuotes")
                settings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes;

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
