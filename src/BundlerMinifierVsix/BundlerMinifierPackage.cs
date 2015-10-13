using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using BundlerMinifier;
using BundlerMinifierVsix.Commands;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BundlerMinifierVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", BundlerMinifier.Constants.VERSION, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidBundlerPackageString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class BundlerMinifierPackage : Package
    {
        public static DTE2 _dte;
        public static Dispatcher _dispatcher;
        public static Package Package;
        private SolutionEvents _solutionEvents;

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;
            _dispatcher = Dispatcher.CurrentDispatcher;
            Package = this;

            Logger.Initialize(this, Constants.VSIX_NAME);
            Telemetry.SetDeviceName(_dte.Edition);

            Events2 events = _dte.Events as Events2;
            _solutionEvents = events.SolutionEvents;

            _solutionEvents.AfterClosing += () => { ErrorList.CleanAllErrors(); };
            _solutionEvents.ProjectRemoved += (project) => { ErrorList.CleanAllErrors(); };

            CreateBundle.Initialize(this);
            UpdateBundle.Initialize(this);
            UpdateAllFiles.Initialize(this);
            BundleOnBuild.Initialize(this);
            RemoveBundle.Initialize(this);

            base.Initialize();
        }

        public static bool IsDocumentDirty(string documentPath, out IVsPersistDocData persistDocData)
        {
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);

            IVsHierarchy vsHierarchy;
            uint itemId, docCookie;
            VsShellUtilities.GetRDTDocumentInfo(
                serviceProvider, documentPath, out vsHierarchy, out itemId, out persistDocData, out docCookie);
            if (persistDocData != null)
            {
                int isDirty;
                persistDocData.IsDocDataDirty(out isDirty);
                return isDirty == 1;
            }

            return false;
        }
    }
}
