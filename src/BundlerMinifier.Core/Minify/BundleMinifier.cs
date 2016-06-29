using System;
using System.Collections.Generic;
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
            string file = bundle.OutputFileName;
            string extension = Path.GetExtension(file).ToUpperInvariant();
            MinificationResult result = null;

            if (!string.IsNullOrEmpty(bundle.Output))
            {
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
            var result = new MinificationResult(file, null, null);

            string minFile = GetMinFileName(file);
            string mapFile = minFile + ".map";

            try
            {
                if (!bundle.SourceMap)
                {
                    UgliflyResult uglifyResult;
                    try
                    {
                        uglifyResult = Uglify.Js(bundle.Output, settings);
                    }
                    catch
                    {
                        uglifyResult = new UgliflyResult(null,
                            new List<UglifyError>{
                                new UglifyError
                                {
                                    IsError = true,
                                    File = file,
                                    Message = "Error processing file"
                                }
                            });
                    }

                    result.MinifiedContent = uglifyResult.Code?.Trim();

                    if (!uglifyResult.HasErrors && !string.IsNullOrEmpty(result.MinifiedContent))
                    {
                        bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);

                        OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);
                        result.Changed |= containsChanges;

                        if (containsChanges)
                        {
                            File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                            OnAfterWritingMinFile(file, minFile, bundle, containsChanges);
                        }

                        GzipFile(minFile, bundle, containsChanges);
                    }
                    else
                    {
                        AddAjaxminErrors(uglifyResult, result);
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
                            sourceMap.SourceRoot = bundle.SourceMapRootPath;

                            if (file.EndsWith(".min.js"))
                            {
                                var inputs = bundle.GetAbsoluteInputFiles();

                                if (inputs.Count == 1)
                                    file = inputs[0];
                            }

                            UgliflyResult uglifyResult;
                            try
                            {
                                uglifyResult = Uglify.Js(bundle.Output, file, settings);
                            }
                            catch
                            {
                                uglifyResult = new UgliflyResult(null,
                                    new List<UglifyError>{
                                        new UglifyError
                                        {
                                            IsError = true,
                                            File = file,
                                            Message = "Error processing file"
                                        }
                                    });
                            }

                            result.MinifiedContent = uglifyResult.Code?.Trim();

                            if (!uglifyResult.HasErrors && !string.IsNullOrEmpty(result.MinifiedContent))
                            {
                                bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);
                                result.Changed |= containsChanges;
                                OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                                if (containsChanges)
                                {
                                    File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                                    OnAfterWritingMinFile(file, minFile, bundle, containsChanges);
                                }


                                GzipFile(minFile, bundle, containsChanges);
                            }
                            else
                            {
                                AddAjaxminErrors(uglifyResult, result);
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
            var settings = CssOptions.GetSettings(bundle);
            string minFile = GetMinFileName(file);
            var result = new MinificationResult(file, null, null);

            try
            {
                UgliflyResult uglifyResult;

                try
                {
                    uglifyResult = Uglify.Css(bundle.Output, file, settings);
                }
                catch
                {
                    uglifyResult = new UgliflyResult(null,
                        new List<UglifyError>{
                                new UglifyError
                                {
                                    IsError = true,
                                    File = file,
                                    Message = "Error processing file"
                                }
                        });
                }
                result.MinifiedContent = uglifyResult.Code?.Trim();

                if (!uglifyResult.HasErrors && !string.IsNullOrEmpty(result.MinifiedContent))
                {
                    bool containsChanges = FileHelpers.HasFileContentChanged(minFile, result.MinifiedContent);
                    result.Changed |= containsChanges;

                    OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                    if (containsChanges)
                    {
                        File.WriteAllText(minFile, result.MinifiedContent, new UTF8Encoding(false));
                        OnAfterWritingMinFile(file, minFile, bundle, containsChanges);
                    }

                    GzipFile(minFile, bundle, containsChanges);
                }
                else
                {
                    AddAjaxminErrors(uglifyResult, result);
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
            var settings = HtmlOptions.GetSettings(bundle);
            string minFile = GetMinFileName(file);

            var minResult = new MinificationResult(file, null, null);

            try
            {
                UgliflyResult uglifyResult;

                try
                {
                    uglifyResult = Uglify.Html(bundle.Output, settings, file);
                }
                catch
                {
                    uglifyResult = new UgliflyResult(null,
                        new List<UglifyError>{
                                new UglifyError
                                {
                                    IsError = true,
                                    File = file,
                                    Message = "Error processing file"
                                }
                        });
                }

                minResult.MinifiedContent = uglifyResult.Code?.Trim();

                if (!uglifyResult.HasErrors && !string.IsNullOrEmpty(minResult.MinifiedContent))
                {
                    bool containsChanges = FileHelpers.HasFileContentChanged(minFile, minResult.MinifiedContent);
                    minResult.Changed |= containsChanges;
                    OnBeforeWritingMinFile(file, minFile, bundle, containsChanges);

                    if (containsChanges)
                    {
                        File.WriteAllText(minFile, minResult.MinifiedContent, new UTF8Encoding(false));
                        OnAfterWritingMinFile(file, minFile, bundle, containsChanges);
                    }

                    GzipFile(minFile, bundle, containsChanges);
                }
                else
                {
                    foreach (var error in uglifyResult.Errors)
                    {
                        minResult.Errors.Add(new MinificationError
                        {
                            FileName = file,
                            Message = error.Message,
                            LineNumber = error.StartLine,
                            ColumnNumber = error.StartColumn
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

                OnAfterWritingGzipFile(sourceFile, gzipFile, bundle, containsChanges);
            }

        }

        internal static void AddAjaxminErrors(UgliflyResult minifier, MinificationResult minResult)
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
