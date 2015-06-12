## Bundler and Minifier

A Visual Studio extension that let's you configure bundling and
minification of JS, CSS and HTML files.

[![Build status](https://ci.appveyor.com/api/projects/status/ktn1qy982qsprwb5?svg=true)](https://ci.appveyor.com/project/madskristensen/bundlerminifier)

Download the extension at the
[VS Gallery](https://visualstudiogallery.msdn.microsoft.com/9ec27da7-e24b-4d56-8064-fd7e88ac1c40)
or get the
[nightly build](http://vsixgallery.com/extension/a0ae318b-4f07-4f71-93cb-f21d3f03c6d3/)

### Features

- Bundles CSS, JavaScript or HTML files into a single output file
- Saving a source file triggers re-bundling automatically
- MSBuild support for CI scenarios supported
- Minify individual or bundled CSS, JavaScript and HTML files
- Minification options for each language is customizable
- Shows a watermark when opening a generated file

### Bundling

Select 2 or more of the same type of files in Solution Explorer
to create a bundle.

![Create bundle](art/contextmenu-createbundle.png)

Any edits being made to the source files will instantly produce
updated bundle file output.

The bundle configuration is stored in a file called `bundleconfig.json`
which gets added to the root of the project.

### Minification

Minify any JS, CSS or HTML file by right-clicking it in Solution
Explorer. That will create a `<filename>.min.<ext>` and nest
it under the original file.

![Minify file](art/contextmenu-minify.png)

When the original file is modified, a new min file is produced
instantly.

### Bundle on build / CI support

In ASP.NET MVC and WebForms projects you can enable bundling and
minification as part of the build step. Simply right-click the
`bundleconfig.json` file to enable it.

![Bundle on build](art/contextmenu-bundleonbuild.png)

Clicking the menu item will prompt you with information about what will
happen if you click the OK button.

![Bundle on build prompt](art/prompt-compileonsave.png)

A NuGet package will be installed into the `packages` folder without adding
any files to the project itself. thead NuGet package contains an MSBuild
task that will run the exact same compilers on the `bundleconfig.json`
file in the root of the project.

### Source maps

Source maps are supported for JavaScript minification only at this time.

A `.map` file is produced next to the `.min.js` file automatically,
but if you manually delete the `.map` file, a new one will not be
created on subsequent minifications.

### bundleconfig.json

The extension adds a `bundleconfig.json` file at the root of the
project which is used to configure all bundling.

Here's an example of what that file looks like:

```js
[
  {
    "outputFileName": "output/bundle.css",
    "inputFiles": [
      "input/file1.css",
      "input/file2.css"
    ],
    "minify": {
			"enabled": true,
			"commentMode": "all"
    },
    "includeInProject": true,
    "sourceMaps": false
  },
  {
    "outputFileName": "output/all.js",
    "inputFiles": [
      "input/file1.js",
      "input/file2.js"
    ],
    "minify": {
			"enabled": true,
			"termSemicolons": true
    },
    "includeInProject": true,
    "sourceMaps": false
  }
]
```