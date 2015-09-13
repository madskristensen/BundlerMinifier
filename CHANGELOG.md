# Roadmap

- [ ] Task Runner Explorer integration
- [ ] Use FileSystemWatcher (#28)

Features that have a checkmark are complete and available for
download in the
[nightly build](http://vsixgallery.com/extension/a0ae318b-4f07-4f71-93cb-f21d3f03c6d3/).

# Changelog

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

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
