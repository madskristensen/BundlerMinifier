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
        public List<string> InputFiles { get; } = new List<string>();

        [JsonProperty("minify")]
        public Dictionary<string, object> Minify { get; } = new Dictionary<string, object> { { "enabled", true } };

        [JsonProperty("includeInProject")]
        public bool IncludeInProject { get; set; } = true;

        [JsonProperty("sourceMap")]
        public bool SourceMap { get; set; }

        [JsonProperty("recursive")]
        public bool Recursive { get; set; }

        internal string Output { get; set; }

        /// <summary>
        /// Converts the relative output file to an absolute file path.
        /// </summary>
        public string GetAbsoluteOutputFile()
        {
            string folder = Path.GetDirectoryName(FileName);
            return Path.Combine(folder, OutputFileName.Replace("/", "\\"));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != GetType()) return false;
            if (obj == this) return true;

            Bundle other = (Bundle)obj;

            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            return OutputFileName.GetHashCode();
        }

        /// <summary>For the JSON.NET serializer</summary>
        public bool ShouldSerializeIncludeInProject()
        {
            Bundle config = new Bundle();
            return IncludeInProject != config.IncludeInProject;
        }

        /// <summary>For the JSON.NET serializer</summary>
        public bool ShouldSerializeMinify()
        {
            Bundle config = new Bundle();
            return !DictionaryEqual(Minify, config.Minify, null);
        }

        private static bool DictionaryEqual<TKey, TValue>(
            IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second,
            IEqualityComparer<TValue> valueComparer)
        {
            if (first == second) return true;
            if ((first == null) || (second == null)) return false;
            if (first.Count != second.Count) return false;

            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                TValue secondValue;
                if (!second.TryGetValue(kvp.Key, out secondValue)) return false;
                if (!valueComparer.Equals(kvp.Value, secondValue)) return false;
            }
            return true;
        }
    }
}
