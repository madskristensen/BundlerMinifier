using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace BundlerMinifierVsix
{
    class TaskRunnerConfig : ITaskRunnerConfig
    {
        private ImageSource _icon;
        private ITaskRunnerCommandContext _context;
        ITaskRunnerNode _hierarchy;

        public TaskRunnerConfig(ITaskRunnerCommandContext context, ITaskRunnerNode hierarchy, ImageSource icon)
        {
            _context = context;
            _hierarchy = hierarchy;
            _icon = icon;
        }

        public ImageSource Icon
        {
            get { return _icon; }
        }

        public ITaskRunnerNode TaskHierarchy
        {
            get { return _hierarchy; }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public string LoadBindings(string configPath)
        {
            string bindingPath = configPath + ".bindings";

            if (File.Exists(bindingPath))
                return File.ReadAllText(bindingPath).Replace("///", string.Empty);

            return "<binding />";
        }

        public bool SaveBindings(string configPath, string bindingsXml)
        {
            string bindingPath = configPath + ".bindings";

            try
            {
                ProjectHelpers.CheckFileOutOfSourceControl(bindingPath);

                if (bindingsXml == "<binding />" && File.Exists(bindingPath))
                {
                    ProjectHelpers.DeleteFileFromProject(bindingPath);
                }
                else
                {
                    File.WriteAllText(bindingPath, "///" + bindingsXml, Encoding.UTF8);
                    ProjectHelpers.AddNestedFile(configPath, bindingPath);
                }

                IVsPersistDocData persistDocData;
                if (!BundlerMinifierPackage.IsDocumentDirty(configPath, out persistDocData) && persistDocData != null)
                {
                    int cancelled;
                    string newName;
                    persistDocData.SaveDocData(VSSAVEFLAGS.VSSAVE_SilentSave, out newName, out cancelled);
                }
                else if(persistDocData == null)
                {
                    new FileInfo(configPath).LastWriteTime = DateTime.Now;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }
    }
}
