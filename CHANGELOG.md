# Roadmap

- [ ] ASP.NET Core TagHelper support
- [ ] Create additional source map for the bundle files
- [ ] Adopt new VS Error List API

# Changelog

These are the changes to each version that has been released
on the official Visual Studio extension gallery

## 2.4

- [x] .csproj based ASP.NET Core apps support bundle-on-build
- [x] BuildBundlerMinifier NuGet package made x-plat

## 2.3

- [x] Updated NuGet package to support .NET Core vNext
- [x] Added support to preserve Knockout containerless bindings and (0, eval) JavaScript expressions (#135 & #63)
- [x] Added "KeepOneSpaceWhenCollapsing" to HTML options (#199)
- [x] Fixed Linux path issues
- [x] Always install latest version of NuGet package

## 2.2

**2016-08-08**

- [x] Various bug fixes
- [x] Support for VS "15"

## 2.1

**2016-06-27**

- [x] Option to suppress output
- [x] Option to disable Task Runner Explorer integration
- [x] Command to convert Gulp
- [x] Only check files out of source control that has changes

## 2.0

**2016-06-16**

- [x] Breaking changes to HTML minification settings
- [x] .NET Core compatible CLI engine
- [x] Brand new CLI story
- [x] JSON schema moved to SchemaStore.org
- [x] Support for VS15
- [x] New button to re-bundle all bundles
- [x] New button to delete all output files
- [x] *Clean* command added to Task Runner Explorer
- [x] *watch* command available in CLI
- [x] Support for outputFileNames containing .min.&lt;ext&gt;
- [x] New [wiki](https://github.com/madskristensen/BundlerMinifier/wiki) content available
- [x] CLI tested on Mac and Linux
- [x] Takes dependency on ASP.NET and Web Tools
- [x] New context menu icons
- [x] New up-to-date screenshots on readme
- [x] Move all strings to .resx files in VSIX project
- [x] Removed options page
- [x] Restart CLI watcher when bundleconfig.json changes
- [x] Show warning on missing input files
- [x] Better error messages in CLI

## 1.9

**2016-05-10**

- [x] Use FileSystemWatcher (#105)
- [x] Make compatible with Web Compiler (#114)
- [x] Support for locked files on "Clean" (#104)
- [x] Added "PreserveCase" to HTML options

## 1.8

**2016-02-05**

- [x] Fixed issue with dynamic .js source files (#99)
- [x] Compiled VSIX for .NET 4.6

## 1.7

**2016-01-19**

- [x] Option to disable bundling on config change (#97)

## 1.6

**2016-01-05**

- [x] Add WhitespaceMinificationMode option (#90)
- [x] No duplicate entries on re-minify (#94)
- [x] Available as a Chocolatey package
- [x] Optimized images/icons

## 1.5

**2015-10-16**

- [x] MSBuild task now uses BuildDependsOn
- [x] MSBuild: Clean task added
- [x] No longer minifies unchanged files
- [x] Don't check unchanged output out of source control (#73)

## 1.4

**2015-09-23**

- [x] Task Runner Explorer integration
- [x] Command line support
- [x] Don't check out files with no changes
- [x] Re-minify support for multiple config files (#49)
- [x] MSBuild task now supports conditions

## 1.3

**2015-09-13**

- [x] Universal Windows Apps support
- [x] Globbing pattern support (#46)
- [x] Added ignore file pattern (#54)
  - InputFiles starting with `!` is ignored
- [x] Support for files with spaces in the path (#55)
- [x] Better minification default options

## 1.2

**2015-08-30**

- [x] All minified files listed in bundleconfig.json
   - You need to re-enable all minified files
- [x] Re-minify all bundles in solution (#27)
- [x] Improve encoding of min files (#33)
- [x] Remove default settings from generated bundles
- [x] Support recursive folder lookup (#36)
- [x] Option for disabling renaming of locals (#32)
   - `renameLocals` option added to `minify` object
- [x] Adjust relative CSS `url()` paths (#34)
   - `adjustRelativePaths` option added to `minify` object

## 1.1

**2015-08-24**

- [x] Bundles CSS, JavaScript or HTML files into a single output file
- [x] Saving a source file triggers re-bundling automatically
- [x] MSBuild support for CI scenarios supported
- [x] Minify individual or bundled CSS, JavaScript and HTML files
- [x] Minification options for each language is customizable
- [x] Shows a watermark when opening a generated file
- [x] Shortcut to update all bundles in solution
