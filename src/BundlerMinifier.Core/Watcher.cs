using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundlerMinifier
{
    internal class Watcher
    {
        private static readonly List<ChangeHandler> ChangeHandlers = new List<ChangeHandler>();
        private static FileSystemWatcher _listener;

        internal static bool Configure(BundleFileProcessor processor, List<string> configurations, string configPath, bool isClean)
        {
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
                        ChangeHandlers.Add(new ChangeHandler(processor, configPath, bundle, isClean));
                    }
                }
            }
            else
            {
                foreach (Bundle bundle in bundles)
                {
                    ChangeHandlers.Add(new ChangeHandler(processor, configPath, bundle, isClean));
                }
            }

            if (ChangeHandlers.Count > 0)
            {
                ConfigureWatcher(configPath);
            }

            return ChangeHandlers.Count > 0;
        }

        private static void ConfigureWatcher(string configPath)
        {
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

        private static void FilesChanged(object sender, FileSystemEventArgs e)
        {
            var fsw = (FileSystemWatcher)sender;
            fsw.EnableRaisingEvents = false;
            bool anyRan = false;

            foreach (ChangeHandler handler in ChangeHandlers)
            {
                anyRan |= handler.FilesChanged(e);
            }

            fsw.EnableRaisingEvents = true;

            if (anyRan)
            {
                Console.WriteLine("Watching... Press [Enter] to stop".LightGray().Bright());
            }
        }
    }
}