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
            File.WriteAllText(configFile + ".defaults", "{}");
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


                bundles = ParseJson(configFile);

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

        private static IEnumerable<Bundle> ParseJson(string configFile)
        {
            configFile = new FileInfo(configFile).FullName;
            string content = File.ReadAllText(configFile);

            var converters = new JsonConverter[] { new BundleJsonConverter(configFile) };
            var result = JsonConvert.DeserializeObject<IEnumerable<Bundle>>(content, converters);
            return result;
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

            foreach (string input in inputFiles)
            {
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

                    sb.AppendLine(content);
                }
            }

            bundle.Output = sb.ToString().Trim();
        }

        private static bool AdjustRelativePaths(Bundle bundle)
        {
            if (!bundle.Minify.ContainsKey("adjustRelativePaths"))
                return true;

            return bundle.Minify["adjustRelativePaths"].ToString() == "True";
        }
    }

    class BundleJsonConverter : JsonConverter
    {
        public JObject DefaultSettings;

        public BundleJsonConverter(string configFile)
        {
            string defaultFile = configFile + ".defaults";

            if (File.Exists(defaultFile))
            {
                DefaultSettings = JObject.Parse(File.ReadAllText(defaultFile));
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);
            if (DefaultSettings != null)
            {
                jObject.Merge(JObject.FromObject(DefaultSettings));
            }

            // Create target object based on JObject
            Bundle target = new Bundle();

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Bundle).IsAssignableFrom(objectType);
        }
    }
}
