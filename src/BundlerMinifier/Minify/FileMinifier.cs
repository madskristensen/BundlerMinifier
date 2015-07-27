using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Ajax.Utilities;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

namespace BundlerMinifier
{
    public class FileMinifier
    {
        public static MinificationResult MinifyFile(string file, bool produceGzipFile, bool produceSourceMap)
        {
            string extension = Path.GetExtension(file).ToUpperInvariant();

            switch (extension)
            {
                case ".JS":
                    return MinifyJavaScript(file, produceGzipFile, produceSourceMap);

                case ".CSS":
                    return MinifyCss(file, produceGzipFile);

                case ".HTML":
                case ".HTM":
                    return MinifyHtml(file, produceGzipFile);
            }

            return null;
        }

        private static MinificationResult MinifyJavaScript(string file, bool produceGzipFile, bool produceSourceMap)
        {
            var settings = new CodeSettings()
            {
                EvalTreatment = EvalTreatment.Ignore,
                TermSemicolons = true,
                PreserveImportantComments = false,
            };

            var minifier = new Minifier();
            var result = new MinificationResult(file, null, null);

            string minFile = GetMinFileName(file);
            string mapFile = minFile + ".map";

            try
            {
                if (!produceSourceMap)
                {
                    result.MinifiedContent = minifier.MinifyJavaScript(File.ReadAllText(file), settings);

                    if (!minifier.Errors.Any())
                    {
                        OnBeforeWritingMinFile(file, minFile);
                        File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                        OnAfterWritingMinFile(file, minFile);
                    }
                    else
                    {
                        AddAjaxminErrors(minifier, result);
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
                                OnBeforeWritingMinFile(file, minFile);
                                File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                                OnAfterWritingMinFile(file, minFile);
                            }
                            else
                            {
                                AddAjaxminErrors(minifier, result);
                            }
                        }

                        result.SourceMap = writer.ToString();
                    }
                }

                if (produceGzipFile)
                    GzipFile(minFile);
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

        private static MinificationResult MinifyCss(string file, bool produceGzipFile)
        {
            string content = File.ReadAllText(file);
            string minFile = GetMinFileName(file);

            var settings = new CssSettings()
            {
                CommentMode = CssComment.Hacks
            };

            var minifier = new Minifier();
            var result = new MinificationResult(file, null, null);

            try
            {
                result.MinifiedContent = minifier.MinifyStyleSheet(content, settings);

                if (!minifier.Errors.Any())
                {
                    OnBeforeWritingMinFile(file, minFile);
                    File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                    OnAfterWritingMinFile(file, minFile);

                    if (produceGzipFile)
                        GzipFile(minFile);
                }
                else
                {
                    AddAjaxminErrors(minifier, result);
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

        private static MinificationResult MinifyHtml(string file, bool produceGzipFile)
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
            var minResult = new MinificationResult(file, null, null);

            try
            {
                MarkupMinificationResult result = minifier.Minify(content, generateStatistics: true);
                minResult.MinifiedContent = result.MinifiedContent;

                if (!result.Errors.Any())
                {
                    OnBeforeWritingMinFile(file, minFile);
                    File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(true));
                    OnAfterWritingMinFile(file, minFile);

                    if (produceGzipFile)
                        GzipFile(minFile);
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

        public static string GetMinFileName(string file)
        {
            string ext = Path.GetExtension(file);
            return file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;
        }

        internal static void AddAjaxminErrors(Minifier minifier, MinificationResult minResult)
        {
            foreach (var error in minifier.ErrorList)
            {
                var minError = new MinificationError
                {
                    FileName = minResult.FileName,
                    Message = error.Message,
                    LineNumber = error.StartLine,
                    ColumnNumber = error.StartColumn
                };

                minResult.Errors.Add(minError);
            }
        }

        private static void GzipFile(string sourceFile)
        {
            var gzipFile = sourceFile + ".gz";
            OnBeforeWritingGzipFile(sourceFile, gzipFile);

            using (var sourceStream = File.OpenRead(sourceFile))
            using (var targetStream = File.OpenWrite(gzipFile))
            using (var gzipStream = new GZipStream(targetStream, CompressionMode.Compress))
                sourceStream.CopyTo(gzipStream);

            OnAfterWritingGzipFile(sourceFile, gzipFile);
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

        protected static void OnBeforeWritingGzipFile(string minFile, string gzipFile)
        {
            if (BeforeWritingGzipFile != null)
            {
                BeforeWritingGzipFile(null, new MinifyFileEventArgs(minFile, gzipFile));
            }
        }

        protected static void OnAfterWritingGzipFile(string minFile, string gzipFile)
        {
            if (AfterWritingGzipFile != null)
            {
                AfterWritingGzipFile(null, new MinifyFileEventArgs(minFile, gzipFile));
            }
        }

        public static event EventHandler<MinifyFileEventArgs> BeforeWritingMinFile;
        public static event EventHandler<MinifyFileEventArgs> AfterWritingMinFile;
        public static event EventHandler<MinifyFileEventArgs> BeforeWritingGzipFile;
        public static event EventHandler<MinifyFileEventArgs> AfterWritingGzipFile;
    }
}
