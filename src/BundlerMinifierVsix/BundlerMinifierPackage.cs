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
        

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;

            CreateBundle.Initialize(this);
            UpdateBundle.Initialize(this);
            MinifyFile.Initialize(this);

            base.Initialize();
        }
    }
}
