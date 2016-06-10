using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BundlerMinifier
{
    internal class Watcher
    {
        private static readonly List<ChangeHandler> ChangeHandlers = new List<ChangeHandler>();
        private static FileSystemWatcher _listener;
        private static string _configPath;
        private static bool _watchingAll;
        private static BundleFileProcessor _processor;

        internal static bool Configure(BundleFileProcessor processor, List<string> configurations, string configPath)
        {
            _processor = processor;

            IEnumerable<Bundle> bundles;

            if (!BundleHandler.TryGetBundles(configPath, out bundles))
            {
                return false;
            }

            if (configurations.Count > 0)
            {
                foreach (string config in configurations)
                {
                    Bundle bundle = bundles.FirstOrDefault(x => string.Equals(x.OutputFileName, config, StringComparison.OrdinalIgnoreCase));

                    if (bundle != null)
                    {
                        ChangeHandlers.Add(new ChangeHandler(processor, configPath, bundle));
                    }
                }
            }
            else
            {
                foreach (Bundle bundle in bundles)
                {
                    ChangeHandlers.Add(new ChangeHandler(processor, configPath, bundle));
                }

                _watchingAll = true;
            }

            if (ChangeHandlers.Count > 0)
            {
                ConfigureWatcher(configPath);
            }

            return ChangeHandlers.Count > 0;
        }

        private static void ConfigureWatcher(string configPath)
        {
            _configPath = configPath;
            string basePath = new FileInfo(configPath).Directory.FullName;
            var fsw = new FileSystemWatcher(basePath);

            fsw.Changed += FilesChanged;
            fsw.Renamed += FilesChanged;

            fsw.IncludeSubdirectories = true;
            fsw.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName;
            fsw.EnableRaisingEvents = true;
            _listener = fsw;
        }

        public static void Stop()
        {
            FileSystemWatcher fsw = _listener;

            if (fsw != null)
            {
                _listener = null;
                fsw.Changed -= FilesChanged;
                fsw.Changed -= FilesChanged;

                try
                {
                    fsw.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private static async void FilesChanged(object sender, FileSystemEventArgs e)
        {
            const int maxRetries = 10;
            var fsw = (FileSystemWatcher)sender;
            fsw.EnableRaisingEvents = false;
            int retries = 0;
            bool suppressOutputMessage = false;

            while (retries < maxRetries)
            {
                await Task.Delay(100);

                try
                {
                    if (string.Equals(e.FullPath, _configPath, StringComparison.OrdinalIgnoreCase))
                    {
                        bool changed = ReloadConfig();
                        suppressOutputMessage = !changed;

                        if (changed)
                        {
                            Console.WriteLine("Configuration reloaded".Green().Bright());
                        }
                    }
                    else
                    {
                        bool anyRan = false;

                        foreach (ChangeHandler handler in ChangeHandlers)
                        {
                            anyRan |= handler.FilesChanged(e);
                        }

                        if (!anyRan)
                        {
                            suppressOutputMessage = true;
                        }
                    }

                    break;
                }
                catch
                {
                    ++retries;
                }
            }

            if(retries >= maxRetries)
            {
                Console.WriteLine("An error occurred while processing".Red().Bright());
            }

            fsw.EnableRaisingEvents = true;

            if (!suppressOutputMessage)
            {
                Console.WriteLine("Watching... Press [Enter] to stop".LightGray().Bright());
            }
        }

        private static bool ReloadConfig()
        {
            bool anyChanges = false;
            IEnumerable<Bundle> bundles;

            if (!BundleHandler.TryGetBundles(_configPath, out bundles))
            {
                throw new Exception("Unable to load bundles.");
            }

            var oldHandlers = ChangeHandlers.ToList();

            if (!_watchingAll)
            {
                foreach (ChangeHandler handler in oldHandlers)
                {
                    Bundle bundle = bundles.FirstOrDefault(x => string.Equals(x.OutputFileName, handler.Bundle.OutputFileName, StringComparison.OrdinalIgnoreCase));

                    if (bundle != null)
                    {
                        ChangeHandler newHandler = new ChangeHandler(_processor, bundle.FileName, bundle);

                        if (!newHandler.Equals(handler))
                        {
                            ChangeHandlers.Remove(handler);
                            ChangeHandlers.Add(newHandler);
                            _processor.Process(_configPath, new[] { bundle });
                            anyChanges = true;
                        }
                    }
                    else
                    {
                        ChangeHandlers.Remove(handler);
                        Console.WriteLine($"Cannot find configuration {handler.Bundle.OutputFileName}".Orange().Bright());
                    }
                }
            }
            else
            {
                HashSet<Bundle> bundlesToProcess = new HashSet<Bundle>(bundles);

                foreach (ChangeHandler handler in oldHandlers)
                {
                    Bundle bundle = bundles.FirstOrDefault(x => string.Equals(x.OutputFileName, handler.Bundle.OutputFileName, StringComparison.OrdinalIgnoreCase));

                    if (bundle != null)
                    {
                        bundlesToProcess.Remove(bundle);
                        ChangeHandler newHandler = new ChangeHandler(_processor, bundle.FileName, bundle);

                        if (!newHandler.Equals(handler))
                        {
                            ChangeHandlers.Remove(handler);
                            ChangeHandlers.Add(newHandler);
                            _processor.Process(_configPath, new[] { bundle });
                            anyChanges = true;
                        }
                    }
                }

                foreach (Bundle bundle in bundlesToProcess)
                {
                    ChangeHandlers.Add(new ChangeHandler(_processor, _configPath, bundle));
                    _processor.Process(_configPath, new[] { bundle });
                    anyChanges = true;
                }
            }

            return anyChanges;
        }
    }
}