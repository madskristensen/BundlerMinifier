using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundlerMinifier
{
    class Program
    {
        private const string DefaultConfigFileName = "bundleconfig.json";

        private static bool GetConfigFileFromArgs(string[] args, out string configPath)
        {
            int index = args.Length - 1;
            IEnumerable<Bundle> bundles;

            if (index > -1)
            {
                if (BundleHandler.TryGetBundles(args[index], out bundles))
                {
                    configPath = args[index];
                    return true;
                }
            }

            if (BundleHandler.TryGetBundles(DefaultConfigFileName, out bundles))
            {
                configPath = new FileInfo(DefaultConfigFileName).FullName;
                return false;
            }

            configPath = null;
            return false;
        }

        static int Main(params string[] args)
        {
            int readConfigsUntilIndex = args.Length;
            string configPath;
            if (GetConfigFileFromArgs(args, out configPath))
            {
                --readConfigsUntilIndex;
            }

            if (configPath == null)
            {
                Console.WriteLine("\x1B[33mUsage: BundlerMinifier [[patterns]] [configPath]");
                Console.WriteLine("\x1B[33mNote:  configPath doesn't need to be specified if bundlerconfig.json exists in the working directory");
                return 0;
            }

            Console.WriteLine($"Running with configuration from {configPath}");

            if (readConfigsUntilIndex <= 0)
            {
                return Run(configPath, null);
            }

            for (int i = 0; i < readConfigsUntilIndex; ++i)
            {
                int runResult = Run(configPath, args[i]);

                if(runResult < 0)
                {
                    return runResult;
                }
            }

            return 0;
        }

        private static int Run(string configPath, string file)
        {
            var configs = GetConfigs(configPath, file);

            if (configs == null)
            {
                Console.WriteLine("\x1B[33mNo configurations matched");
                return -1;
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
                return -1;
            }
        }

        private static void EventHookups(BundleFileProcessor processor, string configPath)
        {
            // For console colors, see http://stackoverflow.com/questions/23975735/what-is-this-u001b9-syntax-of-choosing-what-color-text-appears-on-console

            processor.Processing += (s, e) => { Console.WriteLine($"Processing \x1B[36m{e.Bundle.OutputFileName}"); FileHelpers.RemoveReadonlyFlagFromFile(e.Bundle.GetAbsoluteOutputFile()); };
            processor.AfterBundling += (s, e) => { Console.WriteLine($"  \x1B[32mBundled"); };
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
            {
                return null;
            }

            if (file != null)
            {
                if (file.StartsWith("*"))
                {
                    configs = configs.Where(c => Path.GetExtension(c.OutputFileName).Equals(file.Substring(1), StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    configs = configs.Where(c => c.OutputFileName.Equals(file, StringComparison.OrdinalIgnoreCase));
                }
            }

            return configs;
        }
    }
}
