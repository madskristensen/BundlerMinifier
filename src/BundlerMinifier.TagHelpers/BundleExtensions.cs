using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
#if NETSTANDARD2_0
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace BundlerMinifier.TagHelpers
{
    public static class BundleExtensions
    {
        public static IServiceCollection AddBundles(this IServiceCollection services)
        {
            return AddBundles(services, null);
        }

        public static IServiceCollection AddBundles(this IServiceCollection services, Action<BundleOptions> configure)
        {
            services.AddSingleton<IBundleProvider, BundleProvider>();
            services.AddTransient<BundleOptions>(serviceProvider =>
            {
                var env = serviceProvider.GetService<IWebHostEnvironment>();

                var options = new BundleOptions();
                options.Configure(env);
                configure?.Invoke(options);

                return options;
            });

            return services;
        }
    }
}