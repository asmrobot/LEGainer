using Certes;
using Certes.Acme;
using LEGainer.DNSUtility;
using LEGainer.Handlers;
using LEGainer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer
{
    public class Startup
    {
        private IConfiguration config;
        private ILogger<Startup> logger;

        public Startup(IConfiguration config)
        {
            this.config = config;
            this.logger=LoggerFactory.Create(build => {
                build.AddConsole();
            }).CreateLogger<Startup>();
            //this.logger = logger;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            this.logger.LogInformation("config service");
            //config
            services.Configure<LEGainerOptions>(config.GetSection("legainer"));

            //handler
            services.AddScoped<DownloadCert>();
            services.AddScoped<CreateOrder>();
            services.AddScoped<GenerateCert>();

            services.AddMemoryCache();

            //acme
            services.AddSingleton<AcmeContext>(serviceProvider => new AcmeContext(WellKnownServers.LetsEncryptV2));
            services.AddSingleton<IAccountContext>(serviceProvider => {
                var acme = serviceProvider.GetRequiredService<AcmeContext>();
                var legainerOptions = serviceProvider.GetRequiredService<IOptions<LEGainerOptions>>().Value;
                return acme.NewAccount(legainerOptions.Email, true).Result;
            });
            services.AddSingleton<DNSHelper>();

            //memorycache
            services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            
            //HttpContext
            services.AddHttpContextAccessor();   
        }


        public void Configure(IApplicationBuilder app,IHostApplicationLifetime lifttime, IAccountContext account)
        {
            app.UseDefaultFiles().UseStaticFiles(new StaticFileOptions() { });

            //创建订单，生成 dns challege
            app.Map("/co", appbuilder => appbuilder.Run(async context => {
                await context.RequestServices.GetRequiredService<CreateOrder>().HandleAsync();
            }));

            //验证 dns,并生成证书
            app.Map("/gc", appbuilder => appbuilder.Run(async context => {
                await context.RequestServices.GetRequiredService<GenerateCert>().HandleAsync();
            }));

            //下载证书
            app.Map("/dc", appbuilder => appbuilder.Run(async context =>
            {
                await context.RequestServices.GetRequiredService<DownloadCert>().HandleAsync();
            }));

            app.Run((context) => {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });
        }




    }
}
