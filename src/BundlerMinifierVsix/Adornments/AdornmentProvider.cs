using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.IO;
using BundlerMinifier;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace BundlerMinifierVsix
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("json")]
    [ContentType("javascript")]
    [ContentType("CSS")]
    [ContentType("htmlx")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class AdornmentProvider : IWpfTextViewCreationListener
    {
        private const string _propertyName = "ShowWatermark";
        private const double _initOpacity = 0.3D;
        SettingsManager _settingsManager;

        private static bool _isVisible, _hasLoaded;

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        public SVsServiceProvider serviceProvider { get; set; }

        private void LoadSettings()
        {
            _hasLoaded = true;

            _settingsManager = new ShellSettingsManager(serviceProvider);
            SettingsStore store = _settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            LogoAdornment.VisibilityChanged += AdornmentVisibilityChanged;

            _isVisible = store.GetBoolean(Constants.CONFIG_FILENAME, _propertyName, true);
        }

        private void AdornmentVisibilityChanged(object sender, bool isVisible)
        {
            WritableSettingsStore wstore = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            _isVisible = isVisible;

            if (!wstore.CollectionExists(Constants.CONFIG_FILENAME))
                wstore.CreateCollection(Constants.CONFIG_FILENAME);

            wstore.SetBoolean(Constants.CONFIG_FILENAME, _propertyName, isVisible);
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (!_hasLoaded)
                LoadSettings();

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileName))
                    return;

                CreateAdornments(document, textView);
            }
        }

        private void CreateAdornments(ITextDocument document, IWpfTextView textView)
        {
            string fileName = document.FilePath;

            if (Path.GetFileName(fileName) == Constants.CONFIG_FILENAME)
            {
                LogoAdornment highlighter = new LogoAdornment(textView, _isVisible, _initOpacity);
            }
            else if (Path.IsPathRooted(fileName)) // Check that it's not a dynamic generated file
            {
                var item = BundlerMinifierPackage._dte?.Solution?.FindProjectItem(fileName);

                if (item == null || item.ContainingProject == null)
                    return;

                string configFile = item.ContainingProject.GetConfigFile();

                if (string.IsNullOrEmpty(configFile))
                    return;

                string extension = Path.GetExtension(fileName.Replace(".map", ""));
                string normalizedFilePath = fileName.Replace(".map", "").Replace(".min" + extension, extension);

                try
                {
                    var bundles = BundleHandler.GetBundles(configFile);

                    foreach (Bundle bundle in bundles)
                    {
                        if (bundle.InputFiles.Count == 1 && bundle.InputFiles.First() == bundle.OutputFileName && !fileName.Contains(".min.") && !fileName.Contains(".map"))
                            continue;

                        if (bundle.GetAbsoluteOutputFile().Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            GeneratedAdornment generated = new GeneratedAdornment(textView, _isVisible, _initOpacity);
                            textView.Properties.AddProperty("generated", true);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }
    }
}