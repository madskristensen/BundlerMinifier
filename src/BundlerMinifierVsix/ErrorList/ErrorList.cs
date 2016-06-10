using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using BundlerMinifier;

namespace BundlerMinifierVsix
{
    class ErrorList
    {
        private static Dictionary<string, ErrorListProvider> _providers = new Dictionary<string, ErrorListProvider>();

        public static void AddErrors(string file, IEnumerable<MinificationError> errors)
        {
            CleanErrors(file);

            ErrorListProvider provider = new ErrorListProvider(BundlerMinifierPackage.Package);

            foreach (var error in errors)
            {
                var task = CreateTask(error, provider);
                provider.Tasks.Add(task);
            }

            _providers.Add(file, provider);
        }

        public static void CleanErrors(string file)
        {
            if (_providers.ContainsKey(file))
            {
                _providers[file].Tasks.Clear();
                _providers[file].Dispose();
                _providers.Remove(file);
            }
        }

        public static void CleanAllErrors()
        {
            foreach (string file in _providers.Keys)
            {
                var provider = _providers[file];
                if (provider != null)
                {
                    provider.Tasks.Clear();
                    provider.Dispose();
                }
            }

            _providers.Clear();
        }

        private static ErrorTask CreateTask(MinificationError error, ErrorListProvider provider)
        {
            ErrorTask task = new ErrorTask()
            {
                Line = error.LineNumber,
                Column = error.ColumnNumber,
                ErrorCategory = TaskErrorCategory.Error,
                Category = TaskCategory.Html,
                Document = error.FileName,
                Priority = TaskPriority.Low,
                Text = $"({Vsix.Name}) {error.Message}",
            };

            EnvDTE.ProjectItem item = BundlerMinifierPackage._dte.Solution.FindProjectItem(error.FileName);

            if (item != null && item.ContainingProject != null)
                AddHierarchyItem(task, item.ContainingProject);

            task.Navigate += (s, e) =>
            {
                provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindPrimary));

                if (task.Column > 0)
                {
                    var doc = (EnvDTE.TextDocument)BundlerMinifierPackage._dte.ActiveDocument.Object("textdocument");
                    doc.Selection.MoveToLineAndOffset(task.Line, task.Column, false);
                }
            };

            return task;
        }

        const uint DISP_E_MEMBERNOTFOUND = 0x80020003;

        public static void AddHierarchyItem(ErrorTask task, EnvDTE.Project project)
        {
            IVsHierarchy hierarchyItem = null;
            IVsSolution solution = BundlerMinifierPackage.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            if (solution != null && project != null)
            {
                int flag = -1;

                try
                {
                    flag = solution.GetProjectOfUniqueName(project.FullName, out hierarchyItem);
                }
                catch (COMException ex)
                {
                    if ((uint)ex.ErrorCode != DISP_E_MEMBERNOTFOUND)
                    {
                        throw;
                    }
                }

                if (0 == flag)
                {
                    task.HierarchyItem = hierarchyItem;
                }
            }
        }
    }
}
