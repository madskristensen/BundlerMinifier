using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundlerMinifier
{
    /// <summary>
    /// Represents an error that occured in the compiler.
    /// </summary>
    public class MinificationError
    {
        /// <summary>
        /// The absolute file path of the file being minified.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The error message from the compiler.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The line number in the source file where the error happened.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The column number in the source file where the error happened.
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// The string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Message;
        }
    }
}
