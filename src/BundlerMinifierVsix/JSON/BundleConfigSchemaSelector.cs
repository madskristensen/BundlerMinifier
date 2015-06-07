using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Schema;

namespace BundlerMinifierVsix.JSON
{
    [Export(typeof(IJSONSchemaSelector))]
    class BundleConfigSchemaSelector : IJSONSchemaSelector
    {
        public event EventHandler AvailableSchemasChanged;

        public Task<IEnumerable<string>> GetAvailableSchemasAsync()
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        public string GetSchemaFor(string fileLocation)
        {
            string fileName = Path.GetFileName(fileLocation);

            if (!fileName.Equals(FileHelpers.FILENAME, StringComparison.OrdinalIgnoreCase))
                return null;
            
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            return Path.Combine(folder, "json\\bundleconfig-schema.json");
        }
    }
}
