using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using BundlerMinifier;
using BundlerMinifierVsix.Commands;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BundlerMinifierVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Version, IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidBundlerPackageString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class BundlerMinifierPackage : Package
    {
        public const string Version = "1.0";
        public static DTE2 _dte;
        private static FileProcessor _processor;

        public static FileProcessor Processor
        {
            get
            {
                if (_processor == null)
                {
                    _processor = new FileProcessor();
                    _processor.AfterProcess += AfterProcess;
                    _processor.AfterWritingSourceMap += AfterWritingSourceMap;
                    _processor.BeforeProcess += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.OutputFileName); };
                    _processor.BeforeWritingSourceMap += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
                }

                return _processor;
            }
        }

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;

            CreateBundle.Initialize(this);
            UpdateBundle.Initialize(this);
            MinifyFile.Initialize(this);

            base.Initialize();
        }

        private static void AfterProcess(object sender, BundleFileEventArgs e)
        {
            if (!e.Bundle.IncludeInProject)
                return;

            var item = _dte.Solution.FindProjectItem(e.Bundle.FileName);

            if (item == null || item.ContainingProject == null)
                return;

            ProjectHelpers.AddFileToProject(item.ContainingProject, e.OutputFileName);
            _dte.StatusBar.Text = "Bundle updated";
        }

        private static void BeforeProcess(object sender, BundleFileEventArgs e)
        {
            ProjectHelpers.CheckFileOutOfSourceControl(e.OutputFileName);
        }

        private static void AfterWritingSourceMap(object sender, MinifyFileEventArgs e)
        {
            var item = _dte.Solution.FindProjectItem(e.OriginalFile);

            if (item == null || item.ContainingProject == null)
                return;

            ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);
        }

        private static void BeforeWritingSourceMap(object sender, MinifyFileEventArgs e)
        {
            ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile);
        }
    }
}
