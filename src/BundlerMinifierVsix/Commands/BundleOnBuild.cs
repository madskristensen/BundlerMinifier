﻿using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using Text = BundlerMinifierVsix.Resources.Text;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class BundleOnBuild
    {
        private readonly Package _package;
        private bool _isInstalled;

        private BundleOnBuild(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.BundleOnBuild);
                var menuItem = new OleMenuCommand(EnableCompileOnBuild, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();
            button.Visible = button.Enabled = false;

            if (item == null || item.ContainingProject == null || item.Properties == null)
                return;

            var sourceFile = item.Properties.Item("FullPath").Value.ToString();
            bool isConfigFile = Path.GetFileName(sourceFile).Equals(Constants.CONFIG_FILENAME, StringComparison.OrdinalIgnoreCase);

            if (!isConfigFile)
                return;

            // Some projects don't have a .csproj file and will therefore not be able to execute the build task.
            if (item.ContainingProject.IsKind(ProjectTypes.WEBSITE_PROJECT, ProjectTypes.ASPNET_5))
            {
                button.Visible = button.Enabled = false;
                return;
            }

            button.Visible = button.Enabled = isConfigFile;

            if (button.Visible)
            {
                _isInstalled = IsPackageInstalled(item.ContainingProject);
                button.Checked = _isInstalled;
            }
        }

        public static BundleOnBuild Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return _package;
            }
        }

        public static void Initialize(Package package)
        {
            Instance = new BundleOnBuild(package);
        }

        private void EnableCompileOnBuild(object sender, EventArgs e)
        {
            var item = ProjectHelpers.GetSelectedItems().First();

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));

            if (!_isInstalled)
            {
                var question = MessageBox.Show(Text.NugetInstallPrompt, Vsix.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (question == DialogResult.No)
                    return;

                Version version = null;

                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        string statusInstalling = Text.NugetInstalling.AddParams(Constants.NUGET_ID);
                        BundlerMinifierPackage._dte.StatusBar.Text = statusInstalling;
                        BundlerMinifierPackage._dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationSync);

                        var installer = componentModel.GetService<IVsPackageInstaller2>();
                        installer.InstallPackage(null, item.ContainingProject, Constants.NUGET_ID, version, false);

                        BundlerMinifierPackage._dte.StatusBar.Text = Text.NugetFinishedInstalling.AddParams(Constants.NUGET_ID);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        BundlerMinifierPackage._dte.StatusBar.Text = Text.NugetErrorInstalling.AddParams(Constants.NUGET_ID);
                    }
                    finally
                    {
                        BundlerMinifierPackage._dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationSync);
                    }
                });
            }
            else
            {
                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        BundlerMinifierPackage._dte.StatusBar.Text = Text.NugetUninstalling.AddParams(Constants.NUGET_ID);
                        BundlerMinifierPackage._dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationSync);
                        var uninstaller = componentModel.GetService<IVsPackageUninstaller>();
                        uninstaller.UninstallPackage(item.ContainingProject, Constants.NUGET_ID, false);

                        BundlerMinifierPackage._dte.StatusBar.Text = Text.NugetFinishedUninstalling.AddParams(Constants.NUGET_ID);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        BundlerMinifierPackage._dte.StatusBar.Text = Text.NugetErrorUninstalling.AddParams(Constants.NUGET_ID);
                    }
                    finally
                    {
                        BundlerMinifierPackage._dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationSync);
                    }
                });
            }
        }

        private bool IsPackageInstalled(Project project)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            IVsPackageInstallerServices installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            return installerServices.IsPackageInstalled(project, Constants.NUGET_ID);
        }
    }
}
