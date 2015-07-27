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

        public MinifyFileEventArgs(string originalFile, string resultFile, Bundle bundle)
            : this(originalFile, resultFile)
        {
            Bundle = bundle;
        }

        public string OriginalFile { get; private set; }

        public string ResultFile { get; private set; }

        public Bundle Bundle { get; private set; }

        /// <summary>
        /// A collection of any errors reported by the compiler.
        /// </summary>
        public MinificationResult Result { get; set; }
    }
}
