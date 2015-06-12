using System.IO;
using BundlerMinifier;

namespace BundlerMinifierVsix
{
    public static class FileHelpers
    {
        public static bool HasMinFile(string file, out string minFile)
        {
            minFile = FileMinifier.GetMinFileName(file);

            return File.Exists(minFile);
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
