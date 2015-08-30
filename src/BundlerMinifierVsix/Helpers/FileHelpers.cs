using System.IO;
using BundlerMinifier;

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
            return file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;
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
