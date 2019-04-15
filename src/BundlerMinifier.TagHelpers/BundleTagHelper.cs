using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace BundlerMinifier.TagHelpers
{
    [HtmlTargetElement("bundle")]
    public class BundleTagHelper : TagHelper
    {
        private readonly IBundleProvider _bundleProvider;
        private readonly BundleOptions _options;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IMemoryCache _cache;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private FileVersionProvider _fileVersionProvider;

        public BundleTagHelper(IHostingEnvironment hostingEnvironment, IMemoryCache cache, HtmlEncoder htmlEncoder, IUrlHelperFactory urlHelperFactory, BundleOptions options = null, IBundleProvider bundleProvider = null)
        {
            if (hostingEnvironment == null) throw new ArgumentNullException(nameof(hostingEnvironment));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (htmlEncoder == null) throw new ArgumentNullException(nameof(htmlEncoder));
            if (urlHelperFactory == null) throw new ArgumentNullException(nameof(urlHelperFactory));

            if (options == null)
            {
                options = new BundleOptions();
                options.Configure(hostingEnvironment);
            }

            _bundleProvider = bundleProvider ?? new BundleProvider(hostingEnvironment);
            _options = options;
            _hostingEnvironment = hostingEnvironment;
            _cache = cache;
            _htmlEncoder = htmlEncoder;
            _urlHelperFactory = urlHelperFactory;
        }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        [HtmlAttributeName("name")]
        public string BundleName { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();
            var bundle = _bundleProvider.GetBundle(BundleName);
            if (bundle != null)
            {
                var files = GetFiles(bundle);
                foreach (var file in files)
                {
                    var src = GetSrc(file);
                    if (src == null)
                        continue;

                    if (_options.AppendVersion)
                    {
                        src = GetVersionedSrc(src);
                    }

                    if (bundle.OutputFileUrl.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        output.Content.AppendHtmlLine($"<script src=\"{_htmlEncoder.Encode(src)}\" type=\"text/javascript\"></script>");
                    }
                    else if (bundle.OutputFileUrl.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    {
                        output.Content.AppendHtmlLine($"<link href=\"{_htmlEncoder.Encode(src)}\" rel=\"stylesheet\" />");
                    }
                }
            }
        }

        private IEnumerable<string> GetFiles(Bundle bundle)
        {
            if (_options.UseBundles)
            {
                var bundlePath = bundle.OutputFileUrl;
                if (_options.UseMinifiedFiles)
                {
                    var extension = Path.GetExtension(bundlePath);
                    if (extension != null)
                    {
                        var minifiedPath = Path.ChangeExtension(bundlePath, ".min" + extension);
                        if (File.Exists(minifiedPath))
                        {
                            bundlePath = minifiedPath;
                        }
                    }
                }

                return new[] { bundlePath };
            }

            return bundle.InputFileUrls;
        }

        private string GetVersionedSrc(string srcValue)
        {
            EnsureFileVersionProvider();

            if (_options.AppendVersion)
            {
                srcValue = _fileVersionProvider.AddFileVersionToPath(srcValue);
            }

            return srcValue;
        }

        private void EnsureFileVersionProvider()
        {
            if (_fileVersionProvider == null)
            {
                _fileVersionProvider = new FileVersionProvider(_hostingEnvironment.WebRootFileProvider, _cache, ViewContext.HttpContext.Request.PathBase);
            }
        }

        private string GetSrc(string path)
        {
            var root = FileHelpers.NormalizePath(_hostingEnvironment.WebRootPath.DemandTrailingPathSeparatorChar());
            var filePath = FileHelpers.NormalizePath(path);
            if (filePath.StartsWith(root))
            {
                var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
                return urlHelper.Content("~/" + filePath.Substring(root.Length).Replace('\\', '/'));
            }

            return null;
        }
    }
}
