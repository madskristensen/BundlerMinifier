using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace BundlerMinifierVsix.Listeners
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("json")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class JsonCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                string fileName = Path.GetFileName(_document.FilePath);

                if (fileName.Equals("bundleconfig.json", StringComparison.OrdinalIgnoreCase))
                {
                    _document.FileActionOccurred += DocumentSaved;
                }
            }

            textView.Closed += TextviewClosed;
        }

        private void TextviewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;

            if (view != null)
                view.Closed -= TextviewClosed;

            if (_document != null)
                _document.FileActionOccurred -= DocumentSaved;
        }

        private void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                BundlerMinifierPackage.Processor.Process(e.FilePath);
            }
        }
    }
}
