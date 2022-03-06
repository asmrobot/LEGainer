using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using LEGainer.DNSUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer
{
    /// <summary>
    /// SSL证书生成器
    /// </summary>
    public class SSLGenerate
    {
        public SSLGenerate()
        {}

        public SSLGenerate(AcmeContext acme, IAccountContext account,DNSHelper dnsHelper)
        {
            this.acme = acme;
            this.account = account;
            this.dnsHelper = dnsHelper;
        }


        private AcmeContext acme;
        private IAccountContext account;
        private DNSHelper dnsHelper;

        /// <summary>
        /// DNS质询
        /// </summary>
        private IChallengeContext dnsChallenge;

        /// <summary>
        /// 订单
        /// </summary>
        private IOrderContext order;

        /// <summary>
        /// 私钥匙
        /// </summary>
        private IKey privateKey;

        /// <summary>
        /// 证书
        /// </summary>
        private CertificateChain cert;



        public string Domain { get; set; }

        public string FriendllyDomain 
        {
            get
            {
                return this.Domain.TrimStart('*', '.');
            }
        }


        /// <summary>
        /// 是否已经生成
        /// </summary>
        public bool GeneratedCert { get; set; }

        /// <summary>
        /// 质询域名
        /// </summary>
        public string ChallengeDomain { get; set; }

        /// <summary>
        /// 质询域名的 TXT DNS记录值
        /// </summary>
        public string ChallengRecordValue { get; set; }



        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<bool> CreateOrder(string domain)
        {
            //todo:验证domain和email


            this.Domain = domain;
            try
            {
                this.order = await acme.NewOrder(new[] { Domain });
                var authz = (await this.order.Authorizations()).First();
                this.dnsChallenge = await authz.Dns();

                
                this.ChallengeDomain = $"_acme-challenge.{this.FriendllyDomain}";
                this.ChallengRecordValue = acme.AccountKey.DnsTxt(this.dnsChallenge.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证、生成证书
        /// </summary>
        /// <returns></returns>
        public async Task<(bool,string)> GenerateCert()
        {
            if (GeneratedCert)
            {
                return (true,string.Empty );
            }

            var checkResult = await dnsHelper.CheckDns(this.ChallengeDomain, this.ChallengRecordValue);
            if (!checkResult.Result)
            {
                string msg = "未找到 DNS TXT 解析信息";
                if (checkResult.RecordValues.Count > 0)
                {
                    msg = "未找到正确的解析,当前的解析值有：<br/>";
                    foreach (var item in checkResult.RecordValues)
                    {
                        msg += item + "<br/>";
                    }

                }

                return (false, msg);
            }

            try
            {
                Challenge challenge = await this.dnsChallenge.Validate();

                this.privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                this.cert = await this.order.Generate(new CsrInfo
                {
                    CountryName = "CN",
                    //State = "beijing",
                    //Locality = "beijing",
                    //Organization = "ZTImage",
                    //OrganizationUnit = "Dev",
                    CommonName = this.Domain
                }, this.privateKey);

                this.GeneratedCert = true;
                return (true, "success");
            }
            catch
            {
                return (false, "生成证书时出现问题，请联系我们");
            }
            
        }


        /// <summary>
        /// 获取证书
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> GetCentificateZip()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    //await CompressionData(zip, "readme", "Readme.txt");

                    
                    //pem
                    await CompressionData(zip, this.cert.ToPem(), $"Pem/_{this.FriendllyDomain}_FullChain.pem");
                    await CompressionData(zip, this.privateKey.ToPem(), $"Pem/_{this.FriendllyDomain}_PrivateKey.pem");
                    await CompressionData(zip, this.cert.Certificate.ToPem(), $"Pem/_{this.FriendllyDomain}_Certificate.pem");

                    //Nginx
                    await CompressionData(zip, this.cert.ToPem(), $"Nginx/{this.FriendllyDomain}_cert.crt");
                    await CompressionData(zip, this.privateKey.ToPem(),$"Nginx/{this.FriendllyDomain}_key.key");

                    //Apache


                    //Tomcat


                    //IIS

                }

                return ms.ToArray();
            }


            async Task CompressionData(ZipArchive zip, string pem,string fileName)
            {
                var utf8withoutBom = new System.Text.UTF8Encoding(false);
                byte[] datas= utf8withoutBom.GetBytes(pem.Replace("\r\n", "\n").TrimEnd('\n'));
                ZipArchiveEntry entry = zip.CreateEntry(fileName);
                using (Stream sw = entry.Open())
                {
                    await sw.WriteAsync(datas, 0, datas.Length);
                }
            }

        }

    }
}
