using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix
{
    public class Options : DialogPage
    {
        [Category("General")]
        [DisplayName("Enable Task Runner")]
        [Description("Enables loading tasks into Task Runner Explorer. Restart solution required.")]
        [DefaultValue(true)]
        public bool EnableTaskRunnerExplorer { get; set; } = true;

        [Category("General")]
        [DisplayName("Produce Output Files")]
        [Description("Controls if file system watchers are actively listening to input file changes and bundles and minifies them. Restart of Visual Studio required.")]
        [DefaultValue(true)]
        public bool ProduceOutput { get; set; } = true;
    }
}
