using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer.DNSUtility
{
    public class DNSHelper
    {
        private ILogger<DNSHelper> logger;
        public DNSHelper(ILogger<DNSHelper> logger)
        {
            this.logger = logger;
        }
        public class CheckDnsResult
        {
            public CheckDnsResult()
            {
                this.RecordValues = new List<string>();
            }
            public bool Result { get; set; }
            public List<string> RecordValues { get; set; }
        }
        private static readonly string[] dnsServers = new string[] {
            "114.114.114.114" ,//114DNS
            "1.2.4.8", //CNNIC
            "119.29.29.29",//DNSPod
            "223.5.5.5",//aliDNS
            "8.8.8.8"//Google
        };
        /// <summary>
        /// 检测DNS
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="wantRecordValue"></param>
        /// <returns></returns>
        public async Task<CheckDnsResult> CheckDns(string domain, string wantRecordValue)
        {
            CheckDnsResult result = new CheckDnsResult();
            result.Result = false;
            int offset = 0;
            while (offset < dnsServers.Length)
            {
                var ret = await GetDnsTxtRecord(domain, dnsServers[offset]);
                if (ret.Success)
                {
                    if (ret.RecordValue.Equals(wantRecordValue, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Result = true;
                        return result;
                    }
                    if (!result.RecordValues.Contains(ret.RecordValue))
                    {
                        result.RecordValues.Add(ret.RecordValue);
                    }
                }

               
                offset++;
            }
            return result;
        }

        public async Task<DnsResult> GetDnsTxtRecord(string domain, string dnsServer)
        {
            DnsResult result = new DnsResult();

            var dns = new MyDns();
            if (!await dns.Search(domain, QueryType.TXT, dnsServer, null))
            {
                result.Success = false;
                result.RecordValue = string.Empty;
                switch (dns.header.ResultCode)
                {
                    case ResultCode.OK:
                        result.ErrorMessage = "ok";
                        break;
                    case ResultCode.FormatErr:
                        result.ErrorMessage = "格式错误";
                        break;
                    case ResultCode.DNSErr:
                        result.ErrorMessage = "DNS出错";
                        break;

                    case ResultCode.DomainNoExist:
                        result.ErrorMessage = "域名不存在";
                        break;

                    case ResultCode.DNSNoSuppot:
                        result.ErrorMessage = "DNS不支持这类查询";
                        break;

                    case ResultCode.DNSRefuse:
                        result.ErrorMessage = "DNS拒绝查询";
                        break;
                    default:
                        result.ErrorMessage = "未知错误";
                        break;
                }


                return result;
            }
            else
            {
                result.Success = true;
                result.RecordValue = dns.record?.Records?.FirstOrDefault()?.RDDate?.ToString();

            }

            return result;
        }
    }
}
