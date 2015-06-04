using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundlerMinifierVsix
{
    public static class FileHelpers
    {
        public static bool HasMinFile(string file, out string minFile)
        {
            string ext = Path.GetExtension(file);
            minFile = file.Substring(0, file.LastIndexOf(ext)) + ".min" + ext;

            return File.Exists(minFile);
        }
    }
}
