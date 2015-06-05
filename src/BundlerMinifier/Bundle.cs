using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BundlerMinifier
{
    public class Bundle
    {
        [JsonIgnore]
        public string FileName { get; set; }

        [JsonProperty("outputFileName")]
        public string OutputFileName { get; set; }

        [JsonProperty("inputFiles")]
        public List<string> InputFiles { get; set; } = new List<string>();

        [JsonProperty("minify")]
        public bool Minify { get; set; } = true;

        [JsonProperty("includeInProject")]
        public bool IncludeInProject { get; set; }

        [JsonProperty("sourceMaps")]
        public bool SourceMaps { get; set; }

        internal string Output { get; set; }
    }
}
