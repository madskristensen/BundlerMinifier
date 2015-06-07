using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Schema;

namespace BundlerMinifierVsix.JSON
{
    [Export(typeof(IJSONSchemaFormatHandler))]
    class RelativeFilePathFormatProvider : JSONSchemaFormatHandlerBase
    {
        public override string FormatName
        {
            get { return "bundle_relativepath"; }
        }

        public override bool HideFormatNameCompletion
        {
            get { return true; }
        }
        
        public override IEnumerable<string> GetIssues(JSONDocument doc, string canonicalizedValue)
        {
            if (string.IsNullOrEmpty(doc.DocumentLocation))
                yield break;

            string fileName = Path.GetFileName(doc.DocumentLocation);

            if (!fileName.Equals(FileHelpers.FILENAME, StringComparison.OrdinalIgnoreCase))
                yield break;

            string folder = Path.GetDirectoryName(doc.DocumentLocation);
            string absolutePath = Path.Combine(folder, canonicalizedValue);

            if (!File.Exists(absolutePath) && !Directory.Exists(absolutePath))
                yield return $"The file or directory '{canonicalizedValue}' does not exist";

        }
    }
}
