using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

namespace BundlerMinifier
{
    public class FileMinifier
    {
        public static MinificationResult MinifyFile(string file)
        {
            string extension = Path.GetExtension(file).ToUpperInvariant();

            switch (extension)
            {
                case ".JS":
                    return MinifyJavaScriptWithSourceMap(file);

                case ".CSS":
                    return MinifyCss(file);

                case ".HTML":
                case ".HTM":
                    return MinifyHtml(file);
            }

            return null;
        }

        private static MinificationResult MinifyJavaScriptWithSourceMap(string file)
        {
            var settings = new CodeSettings()
            {
                EvalTreatment = EvalTreatment.Ignore,
                TermSemicolons = true,
                PreserveImportantComments = false,
            };

            var minifier = new Minifier();
            StringWriter writer = new StringWriter();

            string ext = Path.GetExtension(file);
            string minFile = file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;
            string mapFile = minFile + ".map";

            string result = null;

            using (V3SourceMap sourceMap = new V3SourceMap(writer))
            {
                settings.SymbolsMap = sourceMap;
                sourceMap.StartPackage(minFile, mapFile);

                minifier.FileName = file;
                result = minifier.MinifyJavaScript(File.ReadAllText(file), settings);

                if (minifier.Errors.Count == 0)
                {
                    OnBeforeWritingMinFile(file, minFile);
                    File.WriteAllText(minFile, result, new UTF8Encoding(true));
                    OnAfterWritingMinFile(file, minFile);
                }
            }

            return new MinificationResult(result, writer.ToString());
        }

        private static MinificationResult MinifyCss(string file)
        {
            string content = File.ReadAllText(file);
            var result = StringMinifier.MinifyCss(content);

            string minFile = GetMinFileName(file);

            OnBeforeWritingMinFile(file, minFile);
            File.WriteAllText(minFile, result, new UTF8Encoding(true));
            OnAfterWritingMinFile(file, minFile);

            return new MinificationResult(result, null);
        }

        private static MinificationResult MinifyHtml(string file)
        {
            var settings = new HtmlMinificationSettings
            {
                RemoveOptionalEndTags = false,
                AttributeQuotesRemovalMode = WebMarkupMin.Core.HtmlAttributeQuotesRemovalMode.Html5,
                RemoveRedundantAttributes = false,
            };

            string content = File.ReadAllText(file);
            string minFile = GetMinFileName(file);

            var minifier = new HtmlMinifier(settings);
            MarkupMinificationResult result = minifier.Minify(content, generateStatistics: true);
            
            OnBeforeWritingMinFile(file, minFile);
            File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
            OnAfterWritingMinFile(file, minFile);

            return new MinificationResult(result.MinifiedContent, null);
        }

        public static string GetMinFileName(string file)
        {
            string ext = Path.GetExtension(file);
            return file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;
        }

        protected static void OnBeforeWritingMinFile(string file, string minFile)
        {
            if (BeforeWritingMinFile != null)
            {
                BeforeWritingMinFile(null, new MinifyFileEventArgs(file, minFile));
            }
        }

        protected static void OnAfterWritingMinFile(string file, string minFile)
        {
            if (AfterWritingMinFile != null)
            {
                AfterWritingMinFile(null, new MinifyFileEventArgs(file, minFile));
            }
        }

        public static event EventHandler<MinifyFileEventArgs> BeforeWritingMinFile;
        public static event EventHandler<MinifyFileEventArgs> AfterWritingMinFile;
    }
}
