using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace BundlerMinifier
{
    public class Bundler
    {
        public void AddBundle(string fileName, Bundle bundle)
        {
            IEnumerable<Bundle> existing = GetBundles(fileName);
            List<Bundle> bundles = new List<Bundle>();
            bundles.AddRange(existing);
            bundles.Add(bundle);
            bundle.FileName = fileName;

            string content = JsonConvert.SerializeObject(bundles, Formatting.Indented);
            File.WriteAllText(fileName, content);
        }

        public static IEnumerable<Bundle> GetBundles(string fileName)
        {
            FileInfo file = new FileInfo(fileName);

            if (!file.Exists)
                return Enumerable.Empty<Bundle>();

            string content = File.ReadAllText(fileName);
            var bundles = JsonConvert.DeserializeObject<IEnumerable<Bundle>>(content);
            string folder = Path.GetDirectoryName(file.FullName);

            // Make the output path absolute
            foreach (Bundle bundle in bundles)
            {
                bundle.OutputFileName = Path.Combine(folder, bundle.OutputFileName.Replace("/", "\\"));
                bundle.FileName = fileName;
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
                    var files = dir.GetFiles("*" + ext, SearchOption.TopDirectoryOnly);
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
