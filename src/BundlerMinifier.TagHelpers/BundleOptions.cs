using Microsoft.AspNetCore.Hosting;

namespace BundlerMinifier.TagHelpers
{
    public class BundleOptions
    {
        public bool UseBundles { get; set; }
        public bool UseMinifiedFiles { get; set; }
        public bool AppendVersion { get; set; }        

        internal void Configure(IHostingEnvironment env)
        {
            if (env != null)
            {
                var isDevelopment = env.IsDevelopment();
                UseBundles = !isDevelopment;
                UseMinifiedFiles = !isDevelopment;
                AppendVersion = !isDevelopment;
            }
        }
    }
}