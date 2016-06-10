using System.Collections.Generic;

namespace BundlerMinifier
{
    public class MinificationResult
    {
        public MinificationResult(string fileName, string content, string sourceMap)
        {
            FileName = fileName;
            MinifiedContent = content;
            SourceMap = sourceMap;
        }

        public string FileName { get; set; }

        public string MinifiedContent { get; set; }

        /// <summary>
        /// The source map string produced by the compiler.
        /// </summary>
        public string SourceMap { get; set; }

        /// <summary>
        /// A collection of any errors reported by the compiler.
        /// </summary>
        public List<MinificationError> Errors { get; } = new List<MinificationError>();

        /// <summary>
        /// Checks if the compilation resulted in errors.
        /// </summary>
        public bool HasErrors
        {
            get { return Errors.Count > 0; }
        }

        public bool Changed { get; set; }
    }
}
