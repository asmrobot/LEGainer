using LEGainer.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer.Handlers
{
    public class DownloadCert : BaseHandler
    {
        
        private IMemoryCache cache;
        private ILogger<DownloadCert> logger;

        public DownloadCert(IHttpContextAccessor accessor,  IMemoryCache cache,ILogger<DownloadCert> logger):base(accessor)
        {
            this.cache = cache;
            this.logger = logger;
        }


        public async override Task HandleAsync()
        {
            GenerateCertResult result = new GenerateCertResult();

            string sessionKey = this.context.Request.Query["sessionKey"];

            SSLGenerate generate = this.cache.Get<SSLGenerate>(sessionKey);
            if (generate == null)
            {
                this.context.Response.StatusCode = 404;
                await this.context.Response.WriteAsync("你未申请，或申请已过期。");
                return;
            }

            if (!generate.GeneratedCert)
            {
                this.context.Response.StatusCode = 200;
                await this.context.Response.WriteAsync("你的申请还未验证通过");
                return;
            }

            byte[] zipData = await generate.GetCentificateZip();
            context.Response.ContentType = "application/octet-stream";
            context.Response.Headers.Add("Content-Disposition", new string[] { $"attachment;filename={generate.Domain.TrimStart('*')}.zip" });
            await context.Response.Body.WriteAsync(zipData, 0, zipData.Length);
        }
    }
}
