using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using EnvDTE80;

namespace BundlerMinifierVsix
{
    public static class ProjectHelpers
    {
        private static DTE2 _dte = BundlerMinifierPackage._dte;

        public static void CheckFileOutOfSourceControl(string file)
        {
            if (!File.Exists(file) || _dte.Solution.FindProjectItem(file) == null)
                return;

            if (_dte.SourceControl.IsItemUnderSCC(file) && !_dte.SourceControl.IsItemCheckedOut(file))
                _dte.SourceControl.CheckOutItem(file);
        }

        public static IEnumerable<ProjectItem> GetSelectedItems()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                ProjectItem item = selItem.Object as ProjectItem;

                if (item != null)
                    yield return item;
            }
        }

        public static IEnumerable<string> GetSelectedItemPaths()
        {
            foreach (ProjectItem item in GetSelectedItems())
            {
                if (item != null && item.Properties != null)
                    yield return item.Properties.Item("FullPath").Value.ToString();
            }
        }

        public static string GetRootFolder(Project project)
        {
            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }

        public static void AddFileToProject(Project project, string file, string itemType = null)
        {
            if (project.Kind == "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}") // ASP.NET 5 projects
                return;

            try
            {
                ProjectItem item = project.ProjectItems.AddFromFile(file);

                if (string.IsNullOrEmpty(itemType))
                    return;

                item.Properties.Item("ItemType").Value = "None";
            }
            catch { /* Not all project system support adding files to them through the APIs */ }
        }

        public static void AddNestedFile(string parentFile, string newFile)
        {
            ProjectItem item = _dte.Solution.FindProjectItem(parentFile);

            try
            {
                if (item == null || item.ContainingProject == null ||
                    item.ContainingProject.Kind == "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}") // ASP.NET 5 projects
                    return;

                if (item.ProjectItems == null) // Website project
                    item.ContainingProject.ProjectItems.AddFromFile(newFile);
                else
                    item.ProjectItems.AddFromFile(newFile);
            }
            catch { /* Some projects don't support nesting. Ignore the error */ }
        }
    }
}
