using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using LEGainer.DNSUtility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Microsoft.Extensions.Options;

namespace LEGainer
{
    class Program
    {
        static void Main(string[] args)
        {
            //test();
            Host.CreateDefaultBuilder()
                //.ConfigureLogging(logBuilder => {
                //    logBuilder.ClearProviders();
                //    logBuilder.AddNLog("configs/nlog.config");
                //})
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .ConfigureAppConfiguration(configBuilder =>
                    {
                        configBuilder.AddJsonFile("appsettings.json");
                    })
                    .UseUrls("http://+:5080")
                    .UseStartup<Startup>();
                })
                .Build()
                .Run();
        }


        static void test()
        {
            string domainUrl = "tools.ztimage.com";
            string email = "cert@ztimage.com";

            string accountKey = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEICuhTDXKkWbmkLPqFS48HihXntI5ONmRO358iWXjYuSmoAoGCCqGSM49
AwEHoUQDQgAEZpejITnlRdE6Ftaw5FTwTe8FzrZv0ZoZY6BrEDcUOUpEkoSnBKr2
7RdKGqYmv8VujkJlmBpidLNX/HtUyu/G8Q==
-----END EC PRIVATE KEY-----";
            


            var task = Task.Run(async () => {

                AcmeContext acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2, KeyFactory.FromPem(accountKey));
                IAccountContext account = await acme.Account();

                SSLGenerate generate = new SSLGenerate(acme, account,new DNSHelper (null));
                bool result = await generate.CreateOrder(domainUrl);
                if (!result)
                {
                    Console.WriteLine("创建订单失败，10秒后重试");
                    return;
                }
                                
                Console.WriteLine($"添加一条 txt 域名解析记录 域名： {generate.ChallengeDomain}\r\n 值：{generate.ChallengRecordValue}");

                while (true)
                {
                    (var generateResult, var message) = await generate.GenerateCert();
                    if (generateResult)
                    {
                        break;
                    }
                    Console.WriteLine($"生成失败：{message}");
                    await Task.Delay(3000);
                }

                byte[] datas =await generate.GetCentificateZip();

                await File.WriteAllBytesAsync("d:\\certs\\pem.zip", datas);
                Console.WriteLine($"cert ok");
            });

            task.Wait();
            Console.WriteLine("Hello World!");
        }
    }
}
