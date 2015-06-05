using System;

namespace BundlerMinifier
{
    public class MinifyFileEventArgs : EventArgs
    {
        public MinifyFileEventArgs(string originalFile, string resultFile)
        {
            OriginalFile = originalFile;
            ResultFile = resultFile;
        }

        public string OriginalFile { get; set; }

        public string ResultFile { get; set; }

    }
}
