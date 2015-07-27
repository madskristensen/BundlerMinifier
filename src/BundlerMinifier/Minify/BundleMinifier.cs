using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Ajax.Utilities;
using WebMarkupMin.Core.Minifiers;

namespace BundlerMinifier
{
    public class BundleMinifier
    {
        public static MinificationResult MinifyBundle(Bundle bundle)
        {
            string file = bundle.OutputFileName;
            string extension = Path.GetExtension(file).ToUpperInvariant();
            MinificationResult result = null;

            switch (extension)
            {
                case ".JS":
                    result = MinifyJavaScript(bundle);
                    break;
                case ".CSS":
                    result = MinifyCss(bundle);
                    break;
                case ".HTML":
                case ".HTM":
                    result = MinifyHtml(bundle);
                    break;
            }

            if (result != null && result.HasErrors)
            {
                OnErrorMinifyingFile(result);
            }

            return result;
        }

        private static MinificationResult MinifyJavaScript(Bundle bundle)
        {
            string file = bundle.GetAbsoluteOutputFile();
            var settings = JavaScriptOptions.GetSettings(bundle);
            var minifier = new Minifier();
            var result = new MinificationResult(file, null, null);

            string minFile = GetMinFileName(file);
            string mapFile = minFile + ".map";

            try
            {
                if (!bundle.SourceMaps)
                {
                    result.MinifiedContent = minifier.MinifyJavaScript(File.ReadAllText(file), settings);

                    if (!minifier.Errors.Any())
                    {
                        OnBeforeWritingMinFile(file, minFile, bundle);
                        File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                        OnAfterWritingMinFile(file, minFile, bundle);
                    }
                    else
                    {
                        FileMinifier.AddAjaxminErrors(minifier, result);
                    }
                }
                else
                {
                    using (StringWriter writer = new StringWriter())
                    {
                        using (V3SourceMap sourceMap = new V3SourceMap(writer))
                        {
                            settings.SymbolsMap = sourceMap;
                            sourceMap.StartPackage(minFile, mapFile);

                            minifier.FileName = file;
                            result.MinifiedContent = minifier.MinifyJavaScript(File.ReadAllText(file), settings);

                            if (!minifier.Errors.Any())
                            {
                                OnBeforeWritingMinFile(file, minFile, bundle);
                                File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                                OnAfterWritingMinFile(file, minFile, bundle);
                            }
                            else
                            {
                                FileMinifier.AddAjaxminErrors(minifier, result);
                            }
                        }

                        result.SourceMap = writer.ToString();
                    }
                }

                GzipFile(minFile, bundle);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new MinificationError
                {
                    FileName = file,
                    Message = ex.Message,
                    LineNumber = 0,
                    ColumnNumber = 0
                });
            }

            return result;
        }

        private static MinificationResult MinifyCss(Bundle bundle)
        {
            string file = bundle.GetAbsoluteOutputFile();
            string content = File.ReadAllText(file);
            var settings = CssOptions.GetSettings(bundle);
            string minFile = GetMinFileName(file);

            var minifier = new Minifier();
            var result = new MinificationResult(file, null, null);

            try
            {
                result.MinifiedContent = minifier.MinifyStyleSheet(content, settings);

                if (!minifier.Errors.Any())
                {
                    OnBeforeWritingMinFile(file, minFile, bundle);
                    File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                    OnAfterWritingMinFile(file, minFile, bundle);

                    GzipFile(minFile, bundle);
                }
                else
                {
                    FileMinifier.AddAjaxminErrors(minifier, result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new MinificationError
                {
                    FileName = file,
                    Message = ex.Message,
                    LineNumber = 0,
                    ColumnNumber = 0
                });
            }

            return result;
        }

        private static MinificationResult MinifyHtml(Bundle bundle)
        {
            string file = bundle.GetAbsoluteOutputFile();
            string content = File.ReadAllText(file);
            var settings = HtmlOptions.GetSettings(bundle);
            string minFile = GetMinFileName(file);

            var minifier = new HtmlMinifier(settings);
            var minResult = new MinificationResult(file, null, null);

            try
            {
                MarkupMinificationResult result = minifier.Minify(content, generateStatistics: true);
                minResult.MinifiedContent = result.MinifiedContent;

                if (!result.Errors.Any())
                {
                    OnBeforeWritingMinFile(file, minFile, bundle);
                    File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                    OnAfterWritingMinFile(file, minFile, bundle);

                    GzipFile(minFile, bundle);
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        minResult.Errors.Add(new MinificationError
                        {
                            FileName = file,
                            Message = error.Message,
                            LineNumber = error.LineNumber,
                            ColumnNumber = error.ColumnNumber
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                minResult.Errors.Add(new MinificationError
                {
                    FileName = file,
                    Message = ex.Message,
                    LineNumber = 0,
                    ColumnNumber = 0
                });
            }

            return minResult;
        }

        private static void GzipFile(string sourceFile, Bundle bundle)
        {
            if (!bundle.Minify.ContainsKey("gzip") || !bundle.Minify["gzip"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                return;

            var gzipFile = sourceFile + ".gz";
            OnBeforeWritingGzipFile(sourceFile, gzipFile, bundle);

            using (var sourceStream = File.OpenRead(sourceFile))
            using (var targetStream = File.OpenWrite(gzipFile))
            using (var gzipStream = new GZipStream(targetStream, CompressionMode.Compress))
                sourceStream.CopyTo(gzipStream);

            OnAfterWritingGzipFile(sourceFile, gzipFile, bundle);
        }

        public static string GetMinFileName(string file)
        {
            string ext = Path.GetExtension(file);
            return file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;
        }

        protected static void OnBeforeWritingMinFile(string file, string minFile, Bundle bundle)
        {
            if (BeforeWritingMinFile != null)
            {
                BeforeWritingMinFile(null, new MinifyFileEventArgs(file, minFile, bundle));
            }
        }

        protected static void OnAfterWritingMinFile(string file, string minFile, Bundle bundle)
        {
            if (AfterWritingMinFile != null)
            {
                AfterWritingMinFile(null, new MinifyFileEventArgs(file, minFile, bundle));
            }
        }

        protected static void OnBeforeWritingGzipFile(string minFile, string gzipFile, Bundle bundle)
        {
            if (BeforeWritingGzipFile != null)
            {
                BeforeWritingGzipFile(null, new MinifyFileEventArgs(minFile, gzipFile, bundle));
            }
        }

        protected static void OnAfterWritingGzipFile(string minFile, string gzipFile, Bundle bundle)
        {
            if (AfterWritingGzipFile != null)
            {
                AfterWritingGzipFile(null, new MinifyFileEventArgs(minFile, gzipFile, bundle));
            }
        }

        protected static void OnErrorMinifyingFile(MinificationResult result)
        {
            if (ErrorMinifyingFile != null)
            {
                var e = new MinifyFileEventArgs(result.FileName, null, null);
                e.Result = result;

                ErrorMinifyingFile(null, e);
            }
        }

        public static event EventHandler<MinifyFileEventArgs> BeforeWritingMinFile;
        public static event EventHandler<MinifyFileEventArgs> AfterWritingMinFile;
        public static event EventHandler<MinifyFileEventArgs> BeforeWritingGzipFile;
        public static event EventHandler<MinifyFileEventArgs> AfterWritingGzipFile;
        public static event EventHandler<MinifyFileEventArgs> ErrorMinifyingFile;
    }
}
