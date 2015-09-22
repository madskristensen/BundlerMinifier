using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundlerMinifier
{
    class Program
    {
        static int Main(params string[] args)
        {
            string configPath = args[0];
            string file = args.Length > 1 ? args[1] : null;
            var configs = GetConfigs(configPath, file);

            if (configs == null)
            {
                Console.WriteLine("\x1B[33mNo configurations matched");
                return 0;
            }

            BundleFileProcessor processor = new BundleFileProcessor();
            EventHookups(processor, configPath);

            try
            {
                processor.Process(configPath, configs);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1B[33m{ex.Message}");
                return 1;
            }
        }

        private static void EventHookups(BundleFileProcessor processor, string configPath)
        {
            // For console colors, see http://stackoverflow.com/questions/23975735/what-is-this-u001b9-syntax-of-choosing-what-color-text-appears-on-console

            processor.BeforeProcess += (s, e) => { Console.WriteLine($"Processing \x1B[36m{e.Bundle.OutputFileName}"); FileHelpers.RemoveReadonlyFlagFromFile(e.Bundle.GetAbsoluteOutputFile()); };
            processor.AfterProcess += (s, e) => { Console.WriteLine($"  \x1B[32mBundled"); };
            processor.BeforeWritingSourceMap += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            processor.AfterWritingSourceMap += (s, e) => { Console.WriteLine($"  \x1B[32mSourcemapped"); };

            BundleMinifier.BeforeWritingMinFile += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            BundleMinifier.AfterWritingMinFile += (s, e) => { Console.WriteLine($"  \x1B[32mMinified"); };
            BundleMinifier.BeforeWritingGzipFile += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            BundleMinifier.AfterWritingGzipFile += (s, e) => { Console.WriteLine($"  \x1B[32mGZipped"); };
            BundleMinifier.ErrorMinifyingFile += (s, e) => { Console.WriteLine($"{string.Join(Environment.NewLine, e.Result.Errors)}"); };
        }

        private static IEnumerable<Bundle> GetConfigs(string configPath, string file)
        {
            var configs = BundleHandler.GetBundles(configPath);

            if (configs == null || !configs.Any())
                return null;

            if (file != null)
            {
                if (file.StartsWith("*"))
                    configs = configs.Where(c => Path.GetExtension(c.OutputFileName).Equals(file.Substring(1), StringComparison.OrdinalIgnoreCase));
                else
                    configs = configs.Where(c => c.OutputFileName.Equals(file, StringComparison.OrdinalIgnoreCase));
            }

            return configs;
        }
    }
}
