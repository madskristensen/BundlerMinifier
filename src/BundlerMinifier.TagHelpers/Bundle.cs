using System.Collections.Generic;

namespace BundlerMinifier.TagHelpers
{
    public class Bundle
    {
        public string Name { get; set; }
        public string OutputFileUrl { get; set; }
        public IList<string> InputFileUrls { get; set; }
    }
}
