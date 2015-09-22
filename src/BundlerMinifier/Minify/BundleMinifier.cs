using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Ajax.Utilities;
using WebMarkupMin.Core.Minifiers;

namespace BundlerMinifier
{
    public static class BundleMinifier
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

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
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
                if (!bundle.SourceMap)
                {
                    result.MinifiedContent = minifier.MinifyJavaScript(ReadAllText(file), settings).Trim();

                    if (!minifier.Errors.Any())
                    {
                        bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);

                        OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                        if (containsChanges)
                        {
                            File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                        }

                        GzipFile(minFile, bundle, containsChanges);

                        OnAfterWritingMinFile(file, minFile, bundle, containsChanges);
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
                            result.MinifiedContent = minifier.MinifyJavaScript(ReadAllText(file), settings).Trim();

                            if (!minifier.Errors.Any())
                            {
                                bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);

                                OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                                if (containsChanges)
                                {
                                    File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                                }

                                OnAfterWritingMinFile(file, minFile, bundle, containsChanges);

                                GzipFile(minFile, bundle, containsChanges);
                            }
                            else
                            {
                                AddAjaxminErrors(minifier, result);
                            }
                        }

                        result.SourceMap = writer.ToString();
                    }
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

        private static MinificationResult MinifyCss(Bundle bundle)
        {
            string file = bundle.GetAbsoluteOutputFile();
            string content = ReadAllText(file);
            var settings = CssOptions.GetSettings(bundle);
            string minFile = GetMinFileName(file);

            var minifier = new Minifier();
            var result = new MinificationResult(file, null, null);

            try
            {
                result.MinifiedContent = minifier.MinifyStyleSheet(content, settings).Trim();

                if (!minifier.Errors.Any())
                {
                    bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);

                    OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                    if (containsChanges)
                    {
                        File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                    }

                    OnAfterWritingMinFile(file, minFile, bundle, containsChanges);

                    GzipFile(minFile, bundle, containsChanges);
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

        private static MinificationResult MinifyHtml(Bundle bundle)
        {
            string file = bundle.GetAbsoluteOutputFile();
            string content = ReadAllText(file);
            var settings = HtmlOptions.GetSettings(bundle);
            string minFile = GetMinFileName(file);

            var minifier = new HtmlMinifier(settings);
            var minResult = new MinificationResult(file, null, null);

            try
            {
                MarkupMinificationResult result = minifier.Minify(content, generateStatistics: true);
                minResult.MinifiedContent = result.MinifiedContent.Trim();

                if (!result.Errors.Any())
                {
                    bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);

                    OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                    if (containsChanges)
                    {
                        File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                    }

                    OnAfterWritingMinFile(file, minFile, bundle, containsChanges);

                    GzipFile(minFile, bundle, containsChanges);
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

        private static void GzipFile(string sourceFile, Bundle bundle, bool containsChanges)
        {
            if (!bundle.Minify.ContainsKey("gzip") || !bundle.Minify["gzip"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                return;

            var gzipFile = sourceFile + ".gz";

            OnBeforeWritingGzipFile(sourceFile, gzipFile, bundle, containsChanges);

            if (containsChanges)
            {
                using (var sourceStream = File.OpenRead(sourceFile))
                using (var targetStream = File.OpenWrite(gzipFile))
                {
                    var gzipStream = new GZipStream(targetStream, CompressionMode.Compress);
                    sourceStream.CopyTo(gzipStream);
                }
            }

            OnAfterWritingGzipFile(sourceFile, gzipFile, bundle, containsChanges);
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

        public static string GetMinFileName(string file)
        {
            string ext = Path.GetExtension(file);
            return file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;
        }

        public static string ReadAllText(string file)
        {
            using (StreamReader reader = new StreamReader(file, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        public static void OnBeforeWritingMinFile(string file, string minFile, Bundle bundle, bool containsChanges)
        {
            if (BeforeWritingMinFile != null)
            {
                BeforeWritingMinFile(null, new MinifyFileEventArgs(file, minFile, bundle, containsChanges));
            }
        }

        public static void OnAfterWritingMinFile(string file, string minFile, Bundle bundle, bool containsChanges)
        {
            if (AfterWritingMinFile != null)
            {
                AfterWritingMinFile(null, new MinifyFileEventArgs(file, minFile, bundle, containsChanges));
            }
        }

        public static void OnBeforeWritingGzipFile(string minFile, string gzipFile, Bundle bundle, bool containsChanges)
        {
            if (BeforeWritingGzipFile != null)
            {
                BeforeWritingGzipFile(null, new MinifyFileEventArgs(minFile, gzipFile, bundle, containsChanges));
            }
        }

        public static void OnAfterWritingGzipFile(string minFile, string gzipFile, Bundle bundle, bool containsChanges)
        {
            if (AfterWritingGzipFile != null)
            {
                AfterWritingGzipFile(null, new MinifyFileEventArgs(minFile, gzipFile, bundle, containsChanges));
            }
        }

        public static void OnErrorMinifyingFile(MinificationResult result)
        {
            if (ErrorMinifyingFile != null)
            {
                var e = new MinifyFileEventArgs(result.FileName, null, null, false);
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
