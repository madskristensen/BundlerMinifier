using System;
using System.IO;

namespace BundlerMinifierVsix
{
    public static class FileHelpers
    {
        public static bool HasMinFile(string file, out string minFile)
        {
            minFile = GetMinFileName(file);

            return File.Exists(minFile);
        }

        public static string GetMinFileName(string file)
        {
            string ext = Path.GetExtension(file);

            return file.EndsWith(".debug.js")
                ? file.Replace(".debug.", ".")
                : file.Substring(0, file.LastIndexOf(ext, StringComparison.OrdinalIgnoreCase)) + ".min" + ext;
        }

        public static bool HasSourceMap(string file, out string sourceMap)
        {
            if (File.Exists(file + ".map"))
            {
                sourceMap = file + ".map";
                return true;
            }

            sourceMap = null;

            return false;
        }
    }
}
