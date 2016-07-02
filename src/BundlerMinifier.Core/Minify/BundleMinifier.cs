using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;
using NUglify;
using NUglify.JavaScript;

namespace BundlerMinifier
{
    public static class BundleMinifier
    {
        public static MinificationResult MinifyBundle(Bundle bundle)
        {
            string file = bundle.GetAbsoluteOutputFile();
            string extension = Path.GetExtension(file).ToUpperInvariant();
            var minResult = new MinificationResult(file, null, null);

            if (!string.IsNullOrEmpty(bundle.Output) && bundle.IsMinificationEnabled)
            {
                try
                {
                    switch (extension)
                    {
                        case ".JS":
                            MinifyJavaScript(bundle, minResult);
                            break;
                        case ".CSS":
                            MinifyCss(bundle, minResult);
                            break;
                        case ".HTML":
                        case ".HTM":
                            MinifyHtml(bundle, minResult);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AddGenericException(minResult, ex);
                }
            }

            if (minResult.HasErrors)
            {
                OnErrorMinifyingFile(minResult);
            }
            else if (bundle.IsGzipEnabled)
            {
                string minFile = bundle.IsMinificationEnabled ? GetMinFileName(bundle.GetAbsoluteOutputFile()) : bundle.GetAbsoluteOutputFile();
                GzipFile(minFile, bundle, minResult);
            }

            return minResult;
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private static void MinifyJavaScript(Bundle bundle, MinificationResult minResult)
        {
            var settings = JavaScriptOptions.GetSettings(bundle);

            if (!bundle.SourceMap)
            {
                UgliflyResult uglifyResult = Uglify.Js(bundle.Output, settings);
                WriteMinFile(bundle, minResult, uglifyResult);
            }
            else
            {
                string minFile = GetMinFileName(minResult.FileName);
                string mapFile = minFile + ".map";

                using (StringWriter writer = new StringWriter())
                {
                    using (V3SourceMap sourceMap = new V3SourceMap(writer))
                    {
                        settings.SymbolsMap = sourceMap;
                        sourceMap.StartPackage(minFile, mapFile);
                        sourceMap.SourceRoot = bundle.SourceMapRootPath;

                        string file = minResult.FileName;

                        if (bundle.OutputIsMinFile)
                        {
                            var inputs = bundle.GetAbsoluteInputFiles();

                            if (inputs.Count == 1)
                                file = inputs[0];
                        }

                        UgliflyResult uglifyResult = Uglify.Js(bundle.Output, file, settings);
                        WriteMinFile(bundle, minResult, uglifyResult);
                    }

                    minResult.SourceMap = writer.ToString();
                }
            }
        }

        private static void MinifyCss(Bundle bundle, MinificationResult minResult)
        {
            var settings = CssOptions.GetSettings(bundle);

            UgliflyResult uglifyResult = Uglify.Css(bundle.Output, minResult.FileName, settings);
            WriteMinFile(bundle, minResult, uglifyResult);
        }

        private static void MinifyHtml(Bundle bundle, MinificationResult minResult)
        {
            var settings = HtmlOptions.GetSettings(bundle);

            UgliflyResult uglifyResult = Uglify.Html(bundle.Output, settings, minResult.FileName);
            WriteMinFile(bundle, minResult, uglifyResult);
        }

        private static void WriteMinFile(Bundle bundle, MinificationResult minResult, UgliflyResult uglifyResult)
        {
            var minFile = GetMinFileName(minResult.FileName);
            minResult.MinifiedContent = uglifyResult.Code?.Trim();

            if (!uglifyResult.HasErrors)
            {
                bool containsChanges = FileHelpers.HasFileContentChanged(minFile, minResult.MinifiedContent);
                minResult.Changed |= containsChanges;
                OnBeforeWritingMinFile(minResult.FileName, minFile, bundle, containsChanges);

                if (containsChanges)
                {
                    File.WriteAllText(minFile, minResult.MinifiedContent, new UTF8Encoding(false));
                    OnAfterWritingMinFile(minResult.FileName, minFile, bundle, containsChanges);
                }
            }
            else
            {
                AddNUglifyErrors(uglifyResult, minResult);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private static void GzipFile(string sourceFile, Bundle bundle, MinificationResult result)
        {
            var gzipFile = sourceFile + ".gz";
            var containsChanges = result.Changed || File.GetLastWriteTimeUtc(gzipFile) < File.GetLastWriteTimeUtc(sourceFile);

            OnBeforeWritingGzipFile(sourceFile, gzipFile, bundle, containsChanges);

            if (containsChanges)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(result.MinifiedContent??bundle.Output);

                using (var fileStream = File.OpenWrite(gzipFile))
                using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(buffer, 0, buffer.Length);
                }

                OnAfterWritingGzipFile(sourceFile, gzipFile, bundle, containsChanges);
            }
        }

        private static void AddNUglifyErrors(UgliflyResult minifier, MinificationResult minResult)
        {
            foreach (var error in minifier.Errors)
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

        private static void AddGenericException(MinificationResult minResult, Exception ex)
        {
            minResult.Errors.Add(new MinificationError
            {
                FileName = minResult.FileName,
                Message = ex.Message,
                LineNumber = 0,
                ColumnNumber = 0
            });
        }

        public static string GetMinFileName(string file)
        {
            string fileName = Path.GetFileName(file);

            if (fileName.IndexOf(".min.", StringComparison.OrdinalIgnoreCase) > 0)
                return file;

            string ext = Path.GetExtension(file);
            return file.Substring(0, file.LastIndexOf(ext, StringComparison.OrdinalIgnoreCase)) + ".min" + ext;
        }

        static void OnBeforeWritingMinFile(string file, string minFile, Bundle bundle, bool containsChanges)
        {
            BeforeWritingMinFile?.Invoke(null, new MinifyFileEventArgs(file, minFile, bundle, containsChanges));
        }

        static void OnAfterWritingMinFile(string file, string minFile, Bundle bundle, bool containsChanges)
        {
            AfterWritingMinFile?.Invoke(null, new MinifyFileEventArgs(file, minFile, bundle, containsChanges));
        }

        static void OnBeforeWritingGzipFile(string minFile, string gzipFile, Bundle bundle, bool containsChanges)
        {
            BeforeWritingGzipFile?.Invoke(null, new MinifyFileEventArgs(minFile, gzipFile, bundle, containsChanges));
        }

        static void OnAfterWritingGzipFile(string minFile, string gzipFile, Bundle bundle, bool containsChanges)
        {
            AfterWritingGzipFile?.Invoke(null, new MinifyFileEventArgs(minFile, gzipFile, bundle, containsChanges));
        }

        static void OnErrorMinifyingFile(MinificationResult result)
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
