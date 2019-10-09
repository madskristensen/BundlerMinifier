using Microsoft.AspNetCore.Hosting;
#if NETSTANDARD2_0
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.Extensions.Hosting;
#endif

namespace BundlerMinifier.TagHelpers
{
    public class BundleOptions
    {
        public bool UseBundles { get; set; }
        public bool UseMinifiedFiles { get; set; }
        public bool AppendVersion { get; set; }        

        internal void Configure(IWebHostEnvironment env)
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