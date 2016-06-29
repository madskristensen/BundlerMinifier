using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace BundlerMinifier
{
    /// <summary>
    /// Helper class for file interactions
    /// </summary>
    public static class FileHelpers
    {
        /// <summary>
        /// Finds the relative path between two files.
        /// </summary>
        public static string MakeRelative(string baseFile, string file)
        {
            if (string.IsNullOrEmpty(file))
                return file;

            Uri baseUri = new Uri(baseFile, UriKind.RelativeOrAbsolute);
            Uri fileUri = new Uri(file, UriKind.RelativeOrAbsolute);

            if (baseUri.IsAbsoluteUri)
            {
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
            }
            else
            {
                return baseUri.ToString();
            }
        }

        /// <summary>
        /// If a file has the read-only attribute, this method will remove it.
        /// </summary>
        /// <param name="fileName"></param>
        public static void RemoveReadonlyFlagFromFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);

            if (file.Exists && file.IsReadOnly)
                file.IsReadOnly = false;
        }

        /// <summary>
        /// Checks if the content of a file on disk matches the newContent
        /// </summary>
        public static bool HasFileContentChanged(string fileName, string newContent)
        {
            if (!File.Exists(fileName))
                return true;

            string oldContent = File.ReadAllText(fileName);

            return oldContent != newContent;
        }

        public static bool IsUnixPathPreferred
        {
            get
            {
                return Directory.GetCurrentDirectory().IndexOf('/') > -1;
            }
        }

        public static char PathSeparatorChar
        {
            get { return IsUnixPathPreferred ? '/' : '\\'; }
        }

        public static string NormalizePath(this string path)
        {
            bool nix = IsUnixPathPreferred;

            if (nix)
            {
                return path.Replace("\\", "/").Replace("/ ", "\\ ");
            }
            else
            {
                return path.Replace("/", "\\");
            }
        }

        public static string TrimTrailingPathSeparatorChar(this string path)
        {
            bool nix = IsUnixPathPreferred;
            char toTrim = '\\';

            if (nix)
            {
                toTrim = '/';
            }

            return path.NormalizePath().TrimEnd(toTrim);
        }

        public static string DemandTrailingPathSeparatorChar(this string path)
        {
            return path.TrimTrailingPathSeparatorChar() + PathSeparatorChar;
        }

        public static string AsPathSegment(this string path)
        {
            return $"{PathSeparatorChar}{path}{PathSeparatorChar}";
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static string ReadAllText(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 8192, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
