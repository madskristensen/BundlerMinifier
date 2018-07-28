using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BundlerMinifier
{
    public static class BundleHandler
    {
        public static void AddBundle(string configFile, Bundle newBundle)
        {
            IEnumerable<Bundle> existing = GetBundles(configFile)
                .Where(x => !x.OutputFileName.Equals(newBundle.OutputFileName));

            List<Bundle> bundles = new List<Bundle>();

            bundles.AddRange(existing);
            bundles.Add(newBundle);
            newBundle.FileName = configFile;

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };

            string content = JsonConvert.SerializeObject(bundles, settings);
            File.WriteAllText(configFile, content + Environment.NewLine);
        }

        public static void RemoveBundle(string configFile, Bundle bundleToRemove)
        {
            IEnumerable<Bundle> bundles = GetBundles(configFile);
            List<Bundle> newBundles = new List<Bundle>();

            if (bundles.Contains(bundleToRemove))
            {
                newBundles.AddRange(bundles.Where(b => !b.Equals(bundleToRemove)));
                string content = JsonConvert.SerializeObject(newBundles, Formatting.Indented);
                File.WriteAllText(configFile, content);
            }
        }

        public static bool TryGetBundles(string configFile, out IEnumerable<Bundle> bundles)
        {
            try
            {
                if (string.IsNullOrEmpty(configFile) || !File.Exists(configFile))
                {
                    bundles = Enumerable.Empty<Bundle>();
                    return false;
                }

                configFile = new FileInfo(configFile).FullName;
                string content = File.ReadAllText(configFile);
                bundles = JArray.Parse(content).ToObject<Bundle[]>();

                foreach (Bundle bundle in bundles)
                {
                    bundle.FileName = configFile;
                }

                return true;
            }
            catch
            {
                bundles = null;
                return false;
            }
        }

        public static IEnumerable<Bundle> GetBundles(string configFile)
        {
            IEnumerable<Bundle> bundles;
            TryGetBundles(configFile, out bundles);
            return bundles;
        }

        public static void ProcessBundle(string baseFolder, Bundle bundle)
        {
            StringBuilder sb = new StringBuilder();
            List<string> inputFiles = bundle.GetAbsoluteInputFiles();

            for (int i = 0; i < inputFiles.Count; i++)
            {
                var input = inputFiles[i];

                string file = Path.Combine(baseFolder, input);

                if (File.Exists(file))
                {
                    string content;

                    if (input.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && AdjustRelativePaths(bundle))
                    {
                        content = CssRelativePath.Adjust(file, bundle.GetAbsoluteOutputFile());
                    }
                    else
                    {
                        content = FileHelpers.ReadAllText(file);
                    }

                    // adding new line only if there are more than 1 files
                    // otherwise we are preserving file integrity
                    if (sb.Length > 0)
                        sb.AppendLine();

                    sb.Append(content);
                }
            }

            bundle.Output = sb.ToString();
        }

        private static bool AdjustRelativePaths(Bundle bundle)
        {
            if (!bundle.Minify.ContainsKey("adjustRelativePaths"))
                return true;

            return bundle.Minify["adjustRelativePaths"].ToString() == "True";
        }
    }
}
