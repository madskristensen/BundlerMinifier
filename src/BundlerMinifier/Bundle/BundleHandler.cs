using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace BundlerMinifier
{
    public class BundleHandler
    {
        public void AddBundle(string configFile, Bundle bundle)
        {
            IEnumerable<Bundle> existing = GetBundles(configFile);
            List<Bundle> bundles = new List<Bundle>();
            bundles.AddRange(existing);
            bundles.Add(bundle);
            bundle.FileName = configFile;

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };

            string content = JsonConvert.SerializeObject(bundles, settings);
            File.WriteAllText(configFile, content);
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

        public static IEnumerable<Bundle> GetBundles(string configFile)
        {
            FileInfo file = new FileInfo(configFile);

            if (!file.Exists)
                return Enumerable.Empty<Bundle>();

            string content = File.ReadAllText(configFile);
            var bundles = JsonConvert.DeserializeObject<IEnumerable<Bundle>>(content);
            string folder = Path.GetDirectoryName(file.FullName);

            foreach (Bundle bundle in bundles)
            {
                bundle.FileName = configFile;
            }

            return bundles;
        }

        public static void ProcessBundle(string baseFolder, Bundle bundle)
        {
            StringBuilder sb = new StringBuilder();
            List<string> inputFiles = new List<string>();
            string ext = Path.GetExtension(bundle.OutputFileName);

            // Support both directories and specific files
            foreach (string input in bundle.InputFiles)
            {
                string fullPath = Path.Combine(baseFolder, input);

                if (Directory.Exists(fullPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(fullPath);
                    SearchOption search = bundle.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var files = dir.GetFiles("*" + ext, search);
                    inputFiles.AddRange(files.Select(f => f.FullName));
                }
                else
                {
                    inputFiles.Add(fullPath);
                }
            }

            foreach (string  input in inputFiles)
            {
                string file = Path.Combine(baseFolder, input);
                string content = File.ReadAllText(file);
                sb.AppendLine(content);
            }

            bundle.Output = sb.ToString();
        }
    }
}
