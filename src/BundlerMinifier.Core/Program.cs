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
                Console.WriteLine("Usage: BundlerMinifier [[patterns]] [configPath]".Orange().Bright());
                Console.WriteLine("Note:  configPath doesn't need to be specified if bundlerconfig.json exists in the working directory".Orange().Bright());
                return 0;
            }

            Console.WriteLine($"Running with configuration from {configPath}".Green().Bright());

            BundleFileProcessor processor = new BundleFileProcessor();
            EventHookups(processor, configPath);

            List<string> configurations = new List<string>();
            bool isClean = false;
            bool isWatch = false;

            for (int i = 0; i < readConfigsUntilIndex; ++i)
            {
                bool currentArgIsClean = string.Equals(args[i], "clean", StringComparison.OrdinalIgnoreCase);
                bool currentArgIsWatch = string.Equals(args[i], "watch", StringComparison.OrdinalIgnoreCase);

                if (!currentArgIsClean && !currentArgIsWatch)
                {
                    configurations.Add(args[i]);
                }
                else if(currentArgIsClean)
                {
                    isClean = true;
                }
                else
                {
                    isWatch = true;
                }
            }

            if (isClean && isWatch)
            {
                Console.WriteLine("The clean and watch options may not be used together.".Red().Bright());
                return -1;
            }

            if (isWatch)
            {
                bool isWatching = Watcher.Configure(processor, configurations, configPath);

                if(!isWatching)
                {
                    Console.WriteLine("No output file names were matched".Red().Bright());
                    return -1;
                }

                Console.WriteLine("Watching... Press [Enter] to stop".LightGray().Bright());
                Console.ReadLine();
                Watcher.Stop();
                return 0;
            }

            if (configurations.Count == 0)
            {
                return Run(processor, configPath, null, isClean);
            }

            foreach(string config in configurations)
            {
                int runResult = Run(processor, configPath, config, isClean);

                if(runResult < 0)
                {
                    return runResult;
                }
            }

            return 0;
        }

        private static int Run(BundleFileProcessor processor, string configPath, string file, bool isClean)
        {
            var configs = GetConfigs(configPath, file);

            if (configs == null || !configs.Any())
            {
                Console.WriteLine("No configurations matched".Orange().Bright());
                return -1;
            }

            try
            {
                if (isClean)
                {
                    processor.Clean(configPath, configs);
                }
                else
                {
                    processor.Process(configPath, configs);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}".Red().Bright());
                return -1;
            }
        }

        private static void EventHookups(BundleFileProcessor processor, string configPath)
        {
            // For console colors, see http://stackoverflow.com/questions/23975735/what-is-this-u001b9-syntax-of-choosing-what-color-text-appears-on-console

            processor.Processing += (s, e) => { Console.WriteLine($"Processing {e.Bundle.OutputFileName.Cyan().Bright()}"); FileHelpers.RemoveReadonlyFlagFromFile(e.Bundle.GetAbsoluteOutputFile()); };
            processor.AfterBundling += (s, e) => { Console.WriteLine($"  Bundled".Green().Bright()); };
            processor.BeforeWritingSourceMap += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            processor.AfterWritingSourceMap += (s, e) => { Console.WriteLine($"  Sourcemapped".Green().Bright()); };

            BundleMinifier.BeforeWritingMinFile += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            BundleMinifier.AfterWritingMinFile += (s, e) => { Console.WriteLine($"  Minified".Green().Bright()); };
            BundleMinifier.BeforeWritingGzipFile += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            BundleMinifier.AfterWritingGzipFile += (s, e) => { Console.WriteLine($"  GZipped".Green().Bright()); };
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
