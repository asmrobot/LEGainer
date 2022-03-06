using Certes;
using Certes.Acme;
using LEGainer.DNSUtility;
using LEGainer.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace LEGainer.Handlers
{
    public class CreateOrder : BaseHandler
    {
        
        private AcmeContext acme;
        private IAccountContext account;
        private IMemoryCache cache;
        private DNSHelper dnsHelper;
        private ILogger<CreateOrder> logger;

        public CreateOrder(
            IHttpContextAccessor accessor,
            AcmeContext acme,
            IAccountContext account,
            IMemoryCache cache,
            DNSHelper dnsHelper,
            ILogger<CreateOrder> logger
            ):base(accessor)
        {
            this.acme = acme;
            this.account = account;
            this.cache = cache;
            this.dnsHelper = dnsHelper;
            this.logger = logger;
        }

        public async override Task HandleAsync()
        {
            string domain = this.context.Request.Query["domain"];


            //验证
            CreateOrderResult result = new CreateOrderResult();
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"^(\*\.)?([a-zA-Z0-9-]{1,61}\.){0,}[a-zA-Z0-9][a-zA-Z0-9-]{1,61}[a-zA-Z0-9](\.[a-zA-Z]{2,})+$");
            if (!reg.IsMatch(domain))
            {
                result.Success = 0;
                result.Message = "域名格式不正确";
                await this.WriteResultAsync<CreateOrderResult>(200, result);
                return;
            }
            
            result.SessionKey = Guid.NewGuid().ToString();
            SSLGenerate generate = new SSLGenerate(acme, account,dnsHelper);

            bool ret = await generate.CreateOrder(domain);
            if (!ret)
            {
                result.Success = 0;
                result.Message = "创建订单失败，10秒后重试";
            }
            else
            {
                result.Success = 1;
                result.Message = "ok";
                result.ChallengeDomain = generate.ChallengeDomain;
                result.DnsTxtValue = generate.ChallengRecordValue;

                MemoryCacheEntryOptions option = new MemoryCacheEntryOptions();
                option.SetPriority(CacheItemPriority.Normal);
                option.SetSlidingExpiration(TimeSpan.FromHours(24));
                this.cache.Set<SSLGenerate>(result.SessionKey,generate,option);
            }
            

            //序列化结果
            await this.WriteResultAsync<CreateOrderResult>(200, result);
        }
    }
}
