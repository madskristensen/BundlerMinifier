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

        /// <summary>
        /// A collection of any errors reported by the compiler.
        /// </summary>
        public MinificationResult Result { get; set; }
    }
}
