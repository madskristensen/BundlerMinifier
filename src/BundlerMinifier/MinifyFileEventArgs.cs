using System;

namespace BundlerMinifier
{
    public class MinifyFileEventArgs : EventArgs
    {
        public MinifyFileEventArgs(string file, string minFile)
        {
            File = file;
            MinFile = minFile;
        }

        public string File { get; set; }

        public string MinFile { get; set; }

    }
}
