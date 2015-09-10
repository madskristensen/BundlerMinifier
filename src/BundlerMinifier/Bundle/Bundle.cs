using System.Collections.Generic;
using System.Linq;
using System.IO;
using Minimatch;
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

        internal string Output { get; set; }

        /// <summary>
        /// Converts the relative output file to an absolute file path.
        /// </summary>
        public string GetAbsoluteOutputFile()
        {
            string folder = new FileInfo(FileName).DirectoryName;
            return Path.Combine(folder, OutputFileName.Replace("/", "\\"));
        }

        internal List<string> GetAbsoluteInputFiles()
        {
            List<string> files = new List<string>();
            string folder = new DirectoryInfo(Path.GetDirectoryName(FileName)).FullName;
            string ext = Path.GetExtension(InputFiles.First());

            foreach (string inputFile in InputFiles)
            {
                int globIndex = inputFile.IndexOf('*');

                if (globIndex > -1)
                {
                    string relative = string.Empty;
                    int last = inputFile.LastIndexOf('/', globIndex);

                    if (last > -1)
                        relative = inputFile.Substring(0, last + 1);

                    var output = GetAbsoluteOutputFile();
                    var outputMin = BundleFileProcessor.GetMinFileName(output);

                    string searchDir = new FileInfo(Path.Combine(folder, relative).Replace("/", "\\")).FullName;
                    var allFiles = Directory.EnumerateFiles(searchDir, "*" + ext, SearchOption.AllDirectories).Select(f => f.Replace(folder + "\\", ""));

                    var matches = Minimatcher.Filter(allFiles, inputFile, new Options { AllowWindowsPaths = true }).Select(f => Path.Combine(folder, f));
                    matches = matches.Where(match => match != output && match != outputMin);
                    files.AddRange(matches.Where(f => !files.Contains(f)));
                }
                else
                {
                    string fullPath = Path.Combine(folder, inputFile).Replace("/", "\\");

                    if (Directory.Exists(fullPath))
                    {
                        DirectoryInfo dir = new DirectoryInfo(fullPath);
                        SearchOption search = SearchOption.TopDirectoryOnly;
                        var dirFiles = dir.GetFiles("*" + Path.GetExtension(OutputFileName), search);
                        files.AddRange(dirFiles.Select(f => f.FullName).Where(f => !files.Contains(f)));
                    }
                    else
                    {
                        files.Add(fullPath);
                    }
                }
            }

            return files;
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
