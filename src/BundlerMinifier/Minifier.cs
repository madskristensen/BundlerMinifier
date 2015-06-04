using System.IO;
using Microsoft.Ajax.Utilities;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

namespace BundlerMinifier
{
    public class Minifier
    {
        public static void ProcessBundle(Bundle bundle)
        {
            if (!bundle.Minify)
                return;

            string extension = Path.GetExtension(bundle.OutputFileName).ToUpperInvariant();
            string result = null;

            switch (extension)
            {
                case ".JS":
                    result = MinifyJavaScript(bundle.Output);
                    break;

                case ".CSS":
                    result = MinifyCss(bundle.Output);
                    break;

                case ".HTML":
                case ".HTM":
                    result = MinifyHtml(bundle.Output);
                    break;
            }

            if (!string.IsNullOrEmpty(result))
                bundle.Output = result; 
        }

        public static string MinifyFile(string file)
        {
            string extension = Path.GetExtension(file).ToUpperInvariant();
            string content = File.ReadAllText(file);

            switch (extension)
            {
                case ".JS":
                    return MinifyJavaScript(content);

                case ".CSS":
                    return MinifyCss(content);

                case ".HTML":
                case ".HTM":
                    return MinifyHtml(content);
            }

            return null;
        }

        public static string MinifyJavaScript(string content)
        {
            var settings = new CodeSettings()
            {
                EvalTreatment = EvalTreatment.Ignore,
                TermSemicolons = true,
                PreserveImportantComments = false,
            };

            var minifier = new Microsoft.Ajax.Utilities.Minifier();

            string result = minifier.MinifyJavaScript(content, settings);

            if (minifier.Errors.Count == 0)
                return result;

            return null;
        }

        public static string MinifyCss(string content)
        {
            var settings = new CssSettings()
            {
                CommentMode = CssComment.Hacks
            };

            var minifier = new Microsoft.Ajax.Utilities.Minifier();

            string result = minifier.MinifyStyleSheet(content, settings);

            if (minifier.Errors.Count == 0)
                return result;

            return null;
        }

        public static string MinifyHtml(string content)
        {
            var settings = new HtmlMinificationSettings
            {
                RemoveOptionalEndTags = false,
                AttributeQuotesRemovalMode = WebMarkupMin.Core.HtmlAttributeQuotesRemovalMode.Html5,
                RemoveRedundantAttributes = false,
            };

            var minifier = new HtmlMinifier(settings);
            MarkupMinificationResult result = minifier.Minify(content, generateStatistics: true);

            if (result.Errors.Count == 0)
                return result.MinifiedContent;

            return null;
        }
    }
}
