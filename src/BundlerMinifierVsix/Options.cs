using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix
{
    public class Options : DialogPage
    {
        [LocDisplayName("Run on config change")]
        [Description("Runs bundling and minification for all listed files in bundleconfig.json when it is being updated.")]
        [Category("General")]
        [DefaultValue(true)]
        public bool ReRunOnSave { get; set; } = true;
    }
}
