using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using BundlerMinifier;

namespace BundlerMinifierVsix
{
    [TaskRunnerExport(Constants.CONFIG_FILENAME)]
    class BundlerTaskRunner : ITaskRunner
    {
        private static ImageSource _icon;
        private static string _exe;

        public BundlerTaskRunner()
        {
            if (_icon == null || _exe == null)
            {
                string folder = GetExecutableFolder();
                _icon = new BitmapImage(new Uri(Path.Combine(folder, "Resources\\logo.png")));// new BitmapImage(new Uri(@"pack://application:,,,/WebCompilerVsix;component/Resources/logo.png"));
                _exe = Path.Combine(folder, "BundlerMinifier.exe");
            }
        }

        public List<ITaskRunnerOption> Options
        {
            get { return null; }
        }

        public async Task<ITaskRunnerConfig> ParseConfig(ITaskRunnerCommandContext context, string configPath)
        {
            return await Task.Run(() =>
            {
                ITaskRunnerNode hierarchy = LoadHierarchy(configPath);

                return new TaskRunnerConfig(context, hierarchy, _icon);
            });
        }

        private ITaskRunnerNode LoadHierarchy(string configPath)
        {
            ITaskRunnerNode root = new TaskRunnerNode(Vsix.Name);
            TaskRunnerNode tasks = new TaskRunnerNode("All files", true)
            {
                Description = $"Bundles all files listed in {Constants.CONFIG_FILENAME}",
                Command = GetCommand(Path.GetDirectoryName(configPath), $"\"{configPath}\"")
            };

            tasks.Description = $"Bundle configs specified in {Constants.CONFIG_FILENAME}.";
            root.Children.Add(tasks);

            var list = new List<ITaskRunnerNode> {
                GetFileType(configPath, ".js"),
                GetFileType(configPath, ".css"),
                GetFileType(configPath, ".html"),
                GetFileType(configPath, ".htm"),
            };

            root.Children.AddRange(list.Where(i => i != null));

            return root;
        }

        private ITaskRunnerNode GetFileType(string configPath, string extension)
        {
            var configs = BundleHandler.GetBundles(configPath);
            var types = configs?.Where(c => Path.GetExtension(c.OutputFileName).Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (types == null || !types.Any())
                return null;

            string cwd = Path.GetDirectoryName(configPath);
            string friendlyName = GetFriendlyName(extension);

            TaskRunnerNode type = new TaskRunnerNode(friendlyName, true)
            {
                Command = GetCommand(cwd, $"\"{configPath}\"  *{extension}")
            };

            foreach (var config in types)
            {
                TaskRunnerNode child = new TaskRunnerNode(config.OutputFileName, true)
                {
                    Command = GetCommand(cwd, $"\"{configPath}\" \"{config.OutputFileName}\"")
                };

                type.Children.Add(child);
            }

            return type;
        }

        private string GetFriendlyName(string extension)
        {
            switch (extension.ToUpperInvariant())
            {
                case ".CSS":
                    return "Stylesheets";
                case ".JS":
                    return "JavaScript";
                case ".HTML":
                case ".HTM":
                    return "HTML";
            }

            return extension;
        }

        private ITaskRunnerCommand GetCommand(string cwd, string arguments)
        {
            ITaskRunnerCommand command = new TaskRunnerCommand(cwd, _exe, arguments);

            return command;
        }

        private static string GetExecutableFolder()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assembly);
        }
    }
}
