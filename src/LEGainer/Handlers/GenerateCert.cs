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
    public class GenerateCert : BaseHandler
    {
        
        private IMemoryCache cache;
        private ILogger<GenerateCert> logger;

        public GenerateCert(IHttpContextAccessor accessor,  IMemoryCache cache,ILogger<GenerateCert> logger):base(accessor)
        {
            this.cache = cache;
            this.logger = logger;
        }


        public async override Task HandleAsync()
        {
            GenerateCertResult result = new GenerateCertResult();
            result.Success = 0;

            string sessionKey = this.context.Request.Query["sessionKey"];
            SSLGenerate generate = null;
            if (!this.cache.TryGetValue<SSLGenerate>(sessionKey, out generate))
            {
                result.Message = "你的申请信息已经消失,请重新申请";
                await WriteResultAsync(200, result);
                return;
            }

            result.Domain = generate.Domain;
            if (generate.GeneratedCert)
            {
                result.Success = 1;
                result.Message = "ok";
                await WriteResultAsync(200, result);
                return;
            }

            (var ret,var message)=await generate.GenerateCert();
            if (!ret)
            {
                result.Message = message;
            }
            else
            {
                result.Success = 1;
                result.Message = "ok";
            }
            await WriteResultAsync(200, result);
        }
    }
}
