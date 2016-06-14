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
            bool fileExists = false;
            bool fallbackExists = fileExists = File.Exists(DefaultConfigFileName);

            if (index > -1)
            {
                fileExists = File.Exists(args[index]);

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

            if (args.Length > 0)
            {
                if (!fileExists)
                {
                    Console.WriteLine($"A configuration file called {args[index]} could not be found".Red().Bright());
                }
                else
                {
                    Console.WriteLine($"Configuration file {args[index]} has errors".Red().Bright());
                }
            }

            if (!fallbackExists)
            {
                Console.WriteLine($"A configuration file called {DefaultConfigFileName} could not be found".Red().Bright());
            }
            else
            {
                Console.WriteLine($"Configuration file {DefaultConfigFileName} has errors".Red().Bright());
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
                ShowHelp();
                return 0;
            }

            Console.WriteLine($"Running with configuration from {configPath}".Green().Bright());

            BundleFileProcessor processor = new BundleFileProcessor();
            EventHookups(processor, configPath);

            List<string> configurations = new List<string>();
            bool isClean = false;
            bool isWatch = false;
            bool isHelp = false;

            for (int i = 0; i < readConfigsUntilIndex; ++i)
            {
                bool currentArgIsClean = string.Equals(args[i], "clean", StringComparison.OrdinalIgnoreCase);
                bool currentArgIsWatch = string.Equals(args[i], "watch", StringComparison.OrdinalIgnoreCase);
                bool currentArgIsHelp = string.Equals(args[i], "help", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "-h", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "--help", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "help", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "-?", StringComparison.OrdinalIgnoreCase);

                if (!currentArgIsClean && !currentArgIsWatch)
                {
                    configurations.Add(args[i]);
                }
                else if (currentArgIsClean)
                {
                    isClean = true;
                }
                else
                {
                    isWatch = true;
                }
            }

            if (isHelp)
            {
                ShowHelp();
                return 0;
            }

            if (isClean && isWatch)
            {
                Console.WriteLine("The clean and watch options may not be used together.".Red().Bright());
                return -1;
            }

            if (isWatch)
            {
                bool isWatching = Watcher.Configure(processor, configurations, configPath);

                if (!isWatching)
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

            foreach (string config in configurations)
            {
                int runResult = Run(processor, configPath, config, isClean);

                if (runResult < 0)
                {
                    return runResult;
                }
            }

            return 0;
        }

        private static void ShowHelp()
        {
#if DOTNET
            const string commandName = "dotnet bundle";
#else
            const string commandName = "BundlerMinifier";
#endif
            using (ColoredTextRegion.Create(s => s.Orange().Bright()))
            {
                Console.WriteLine($"Usage: {commandName} [[args]] [configPath]");
                Console.WriteLine(" Each arg in args can be one of the following:");
                Console.WriteLine("     - The name of an output to process (outputFileName in the configuration file)");
                Console.WriteLine("         If no outputs to process are specified, all ");
                Console.WriteLine("     - [ -? | -h | --help | help]        - Shows this help message");
                Console.WriteLine("         All other arguments are ignored when one of the help switches are included");
                Console.WriteLine("     - clean                             - Deletes artifacts from previous runs");
                Console.WriteLine("         All other arguments are ignored when \"clean\" is included");
                Console.WriteLine("         Not compatible with \"watch\"");
                Console.WriteLine("     - watch                             - Deletes artifacts from previous runs");
                Console.WriteLine("         Watches files that would cause specified rules to run");
                Console.WriteLine("         Not compatible with \"clean\"");
                Console.WriteLine("     - [ -? | -h | --help ] to show this help message");
                Console.WriteLine($" The configPath paramter may be omitted if a {DefaultConfigFileName} file is in the working directory");
                Console.WriteLine("     otherwise, this parameter must be the location of a file containing the definitions for how");
                Console.WriteLine("     the bundling and minification should be performed.");
            }
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
