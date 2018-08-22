using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using BundlerMinifier;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    class ProjectEventCommand
    {
        private IServiceProvider _provider;
        private ConcurrentDictionary<Project, FileSystemWatcher> _listeners;
        private SolutionEvents _events;
        private string[] _ignorePatterns = { "\\node_modules\\", "\\bower_components\\", "\\jspm_packages\\" };
        Timer _timer;
        private ConcurrentDictionary<string, QueueItem> _queue = new ConcurrentDictionary<string, QueueItem>();

        private ProjectEventCommand(IServiceProvider provider)
        {
            _provider = provider;
            _listeners = new ConcurrentDictionary<Project, FileSystemWatcher>();

            var dte = (DTE2)provider.GetService(typeof(DTE));
            _events = dte.Events.SolutionEvents;

            if (dte.Solution.IsOpen)
            {
                OnSolutionOpened();
            }

            _events.Opened += OnSolutionOpened;
            _events.BeforeClosing += OnSolutionClosing;
            _events.ProjectAdded += EnsureProjectIsActive;
            _events.ProjectRemoved += OnProjectRemoved;

            _timer = new Timer(TimerElapsed, null, 0, 250);
        }

        public static ProjectEventCommand Instance { get; private set; }

        public static void Initialize(Package provider)
        {
            Instance = new ProjectEventCommand(provider);
        }

        private void OnSolutionOpened()
        {
            try
            {
                var projects = ProjectHelpers.GetAllProjects();

                foreach (var project in projects)
                {
                    EnsureProjectIsActive(project);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OnSolutionClosing()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                OnProjectRemoved(_listeners.ElementAt(i).Key);
            }
        }

        /// <summary>Starts the file system watcher on the project root folder if it isn't already running.</summary>
        public void EnsureProjectIsActive(Project project)
        {
            if (project == null || _listeners.ContainsKey(project))
                return;

            var config = project.GetConfigFile();

            if (!BundleService.IsOutputProduced(config))
                return;

            try
            {
                if (!string.IsNullOrEmpty(config) && File.Exists(config))
                {
                    var fsw = new FileSystemWatcher(project.GetRootFolder());

                    fsw.Changed += FileChanged;
                    fsw.Renamed += FileChanged;

                    fsw.IncludeSubdirectories = true;
                    fsw.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName;
                    fsw.EnableRaisingEvents = true;

                    _listeners.TryAdd(project, fsw);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

        }

        private void OnProjectRemoved(Project project)
        {
            if (project == null || !_listeners.ContainsKey(project))
                return;

            try
            {
                FileSystemWatcher fsw;

                if (_listeners.TryRemove(project, out fsw))
                {
                    fsw.EnableRaisingEvents = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        void FileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (!IsFileValid(e.FullPath))
                    return;

                var fsw = (FileSystemWatcher)sender;
                fsw.EnableRaisingEvents = false;

                var project = _listeners.Keys.FirstOrDefault(p => e.FullPath.StartsWith(p.GetRootFolder()));

                if (project != null)
                {
                    string configFile = project.GetConfigFile();
                    _queue[e.FullPath] = new QueueItem { ConfigFile = configFile };
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private bool IsFileValid(string file)
        {
            try
            {
                string fileName = Path.GetFileName(file);

                // VS adds ~ to temp file names so let's ignore those
                if (fileName.Contains('~') || fileName.Contains(".min."))
                    return false;

                if (_ignorePatterns.Any(p => file.IndexOf(p) > -1))
                {
                    //var fsw = (FileSystemWatcher)sender;
                    //fsw.EnableRaisingEvents = false;
                    return false;
                }

                if (!BundleFileProcessor.IsSupported(file))
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        void TimerElapsed(object state)
        {
            try
            {
                var items = _queue.Where(i => i.Value.Timestamp < DateTime.Now.AddMilliseconds(-250));

                foreach (var item in items)
                {
                    BundleService.SourceFileChanged(item.Value.ConfigFile, item.Key);
                }

                foreach (var item in _queue)
                {
                    QueueItem old;
                    if (item.Value.Timestamp < DateTime.Now.AddMilliseconds(-250))
                    {
                        _queue.TryRemove(item.Key, out old);
                    }
                }

                foreach (var fsw in _listeners.Values)
                {
                    fsw.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        class QueueItem
        {
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string ConfigFile { get; set; }
        }
    }
}