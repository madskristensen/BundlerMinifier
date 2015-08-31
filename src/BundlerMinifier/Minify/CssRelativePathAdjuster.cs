using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BundlerMinifier
{
    static class CssRelativePath
    {
        private static readonly Regex _rxUrl = new Regex(@"url\s*\(\s*([""']?)([^:)]+)\1\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Adjust(string inputFile, string outputPath)
        {
            string cssFileContents = BundleMinifier.ReadAllText(inputFile);
            string absoluteOutputPath = new FileInfo(outputPath).FullName;

            // apply the RegEx to the file (to change relative paths)
            var matches = _rxUrl.Matches(cssFileContents);

            // Ignore the file if no match
            if (matches.Count > 0)
            {
                string cssDirectoryPath = Path.GetDirectoryName(inputFile);

                foreach (Match match in matches)
                {
                    string quoteDelimiter = match.Groups[1].Value; //url('') vs url("")
                    string relativePathToCss = match.Groups[2].Value;

                    // Ignore root relative references
                    if (relativePathToCss.StartsWith("/", StringComparison.Ordinal))
                        continue;

                    //prevent querystring from causing error
                    var pathAndQuery = relativePathToCss.Split(new[] { '?' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    var pathOnly = pathAndQuery[0];
                    var queryOnly = pathAndQuery.Length == 2 ? pathAndQuery[1] : string.Empty;

                    string absolutePath = GetAbsolutePath(cssDirectoryPath, pathOnly);
                    string serverRelativeUrl = BundlerBuildTask.MakeRelative(absoluteOutputPath, absolutePath);

                    if (!string.IsNullOrEmpty(queryOnly))
                        serverRelativeUrl += "?" + queryOnly;

                    string replace = string.Format("url({0}{1}{0})", quoteDelimiter, serverRelativeUrl);

                    cssFileContents = cssFileContents.Replace(match.Groups[0].Value, replace);
                }
            }

            return cssFileContents;
        }

        private static string GetAbsolutePath(string cssFilePath, string pathOnly)
        {
            return Path.GetFullPath(Path.Combine(cssFilePath, pathOnly));
        }
    }
}