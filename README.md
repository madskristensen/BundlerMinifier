## Bundler and Minifier

A Visual Studio extension that let's you configure bundling and 
minification of JS, CSS and HTML files.

### Bundling

![Create bundle](art\contextmenu-createbundle.png)

### Minification

![Minify file](art\contextmenu-minify.png)

### bundleconfig.json

The extension adds a `bundleconfig.json` file at the root of the
project which is used to configure all bundling.

Here's an example of what that file looks like:

```json
[
	{
		"outputFileName": "output/bundle.css",
		"inputFiles": [
			"input/file1.css",
			"input/file2.css"
		],
		"minify": false,
		"includeInProject": true
	},
	{
		"outputFileName": "output/bundle.js",
		"inputFiles": [
			"input/file1.js",
			"input/file2.js"
		],
		"minify": true,
		"includeInProject": true
	}
]
```