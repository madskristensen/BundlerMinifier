using System;

namespace BundlerMinifier
{
    public class MinifyFileEventArgs : EventArgs
    {
        public MinifyFileEventArgs(string originalFile, string resultFile, bool containsChanges)
        {
            ContainsChanges = containsChanges;
            OriginalFile = originalFile;
            ResultFile = resultFile;
        }

        public MinifyFileEventArgs(string originalFile, string resultFile, Bundle bundle, bool containsChanges)
            : this(originalFile, resultFile, containsChanges)
        {
            Bundle = bundle;
        }

        public bool ContainsChanges { get; set; }

        public string OriginalFile { get; private set; }

        public string ResultFile { get; private set; }

        public Bundle Bundle { get; private set; }

        /// <summary>
        /// A collection of any errors reported by the compiler.
        /// </summary>
        public MinificationResult Result { get; set; }
    }
}
