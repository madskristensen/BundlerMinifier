﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BundlerMinifier;

namespace BundlerMinifierConsole
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
                    System.Console.WriteLine($"A configuration file called {args[index]} could not be found".Red().Bright());
                }
                else
                {
                    System.Console.WriteLine($"Configuration file {args[index]} has errors".Red().Bright());
                }
            }

            if (!fallbackExists)
            {
                System.Console.WriteLine($"A configuration file called {DefaultConfigFileName} could not be found".Red().Bright());
            }
            else
            {
                System.Console.WriteLine($"Configuration file {DefaultConfigFileName} has errors".Red().Bright());
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

            System.Console.WriteLine($"Bundling with configuration from {configPath}".Green().Bright());

            BundleFileProcessor processor = new BundleFileProcessor();
            EventHookups(processor, configPath);

            List<string> configurations = new List<string>();
            bool isClean = false;
            bool isWatch = false;
            bool isNoColor = false;
            bool isHelp = false;

            for (int i = 0; i < readConfigsUntilIndex; ++i)
            {
                bool currentArgIsClean = string.Equals(args[i], "clean", StringComparison.OrdinalIgnoreCase);
                bool currentArgIsWatch = string.Equals(args[i], "watch", StringComparison.OrdinalIgnoreCase);
                bool currentArgIsNoColor = string.Equals(args[i], "--no-color", StringComparison.OrdinalIgnoreCase);
                bool currentArgIsHelp = string.Equals(args[i], "help", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "-h", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "--help", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "help", StringComparison.OrdinalIgnoreCase);
                currentArgIsHelp |= string.Equals(args[i], "-?", StringComparison.OrdinalIgnoreCase);

                if (currentArgIsHelp)
                {
                    isHelp = true;
                    break;
                }
                else if (currentArgIsClean)
                {
                    isClean = true;
                }
                else if (currentArgIsWatch)
                {
                    isWatch = true;
                }
                else if (currentArgIsNoColor)
                {
                    isNoColor = true;
                }
                else
                {
                    configurations.Add(args[i]);
                }
            }

            if (isNoColor)
            {
                StringExtensions.NoColor = true;
            }

            if (isHelp)
            {
                ShowHelp();
                return 0;
            }

            if (isClean && isWatch)
            {
                System.Console.WriteLine("The clean and watch options may not be used together.".Red().Bright());
                return -1;
            }

            if (isWatch)
            {
                bool isWatching = Watcher.Configure(processor, configurations, configPath);

                if (!isWatching)
                {
                    System.Console.WriteLine("No output file names were matched".Red().Bright());
                    return -1;
                }

                System.Console.WriteLine("Watching... Press [Enter] to stop".LightGray().Bright());
                System.Console.ReadLine();
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
                System.Console.WriteLine($"Usage: {commandName} [[args]] [configPath]");
                System.Console.WriteLine(" Each arg in args can be one of the following:");
                System.Console.WriteLine("     - The name of an output to process (outputFileName in the configuration file)");
                System.Console.WriteLine("         If no outputs to process are specified, all ");
                System.Console.WriteLine("     - [ -? | -h | --help | help]        - Shows this help message");
                System.Console.WriteLine("         All other arguments are ignored when one of the help switches are included");
                System.Console.WriteLine("     - clean                             - Deletes artifacts from previous runs");
                System.Console.WriteLine("         All other arguments are ignored when \"clean\" is included");
                System.Console.WriteLine("         Not compatible with \"watch\"");
                System.Console.WriteLine("     - watch                             - Deletes artifacts from previous runs");
                System.Console.WriteLine("         Watches files that would cause specified rules to run");
                System.Console.WriteLine("         Not compatible with \"clean\"");
                System.Console.WriteLine("     - --no-color                        - Doesn't colorize output");
                System.Console.WriteLine("     - [ -? | -h | --help ] to show this help message");
                System.Console.WriteLine($" The configPath parameter may be omitted if a {DefaultConfigFileName} file is in the working directory");
                System.Console.WriteLine("     otherwise, this parameter must be the location of a file containing the definitions for how");
                System.Console.WriteLine("     the bundling and minification should be performed.");
            }
        }

        private static int Run(BundleFileProcessor processor, string configPath, string file, bool isClean)
        {
            var configs = GetConfigs(configPath, file);

            if (configs == null || !configs.Any())
            {
                System.Console.WriteLine("No configurations matched".Orange().Bright());
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
                System.Console.WriteLine($"{ex.Message}".Red().Bright());
                return -1;
            }
        }

        private static void EventHookups(BundleFileProcessor processor, string configPath)
        {
            // For console colors, see http://stackoverflow.com/questions/23975735/what-is-this-u001b9-syntax-of-choosing-what-color-text-appears-on-console

            processor.Processing += (s, e) => { System.Console.WriteLine($"Processing {e.Bundle.OutputFileName.Cyan().Bright()}"); FileHelpers.RemoveReadonlyFlagFromFile(e.Bundle.GetAbsoluteOutputFile()); };
            processor.AfterBundling += (s, e) => { System.Console.WriteLine($"  Bundled".Green().Bright()); };
            processor.BeforeWritingSourceMap += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            processor.AfterWritingSourceMap += (s, e) => { System.Console.WriteLine($"  Sourcemapped".Green().Bright()); };

            BundleMinifier.BeforeWritingMinFile += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            BundleMinifier.AfterWritingMinFile += (s, e) => { System.Console.WriteLine($"  Minified".Green().Bright()); };
            BundleMinifier.BeforeWritingGzipFile += (s, e) => { FileHelpers.RemoveReadonlyFlagFromFile(e.ResultFile); };
            BundleMinifier.AfterWritingGzipFile += (s, e) => { System.Console.WriteLine($"  GZipped".Green().Bright()); };
            BundleMinifier.ErrorMinifyingFile += (s, e) => { System.Console.WriteLine($"{string.Join(Environment.NewLine, e.Result.Errors)}"); };
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
