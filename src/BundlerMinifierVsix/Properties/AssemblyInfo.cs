using System.Reflection;
using System.Runtime.InteropServices;
using BundlerMinifierVsix;

[assembly: AssemblyTitle(Constants.VSIX_NAME)]
[assembly: AssemblyDescription("Adds support for bundling and minifying JavaScript, CSS and HTML files in any project")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Mads Kristensen")]
[assembly: AssemblyProduct(Constants.VSIX_NAME)]
[assembly: AssemblyCopyright("Mads Kristensen")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("en-US")]
[assembly: ComVisible(false)]

[assembly: AssemblyVersion(BundlerMinifierPackage.Version)]
[assembly: AssemblyFileVersion(BundlerMinifierPackage.Version)]
