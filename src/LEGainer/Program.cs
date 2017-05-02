using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LEGainer.Utils;
using ACMESharp.Vault.Model;
using System.IO;


namespace LEGainer
{
    class Program
    {

        public static Guid Initialize()
        {
            string baseuri = ACMESharpUtils.WELL_KNOWN_BASE_SERVICES[ACMESharpUtils.WELL_KNOWN_LE];
            Guid id = ACMESharp.Vault.Util.EntityHelper.NewId();
            using (var vlt = ACMESharpUtils.GetVault())
            {
                Console.WriteLine("Initializing Storage Backend");
                vlt.InitStorage(true);
                var v = new VaultInfo
                {
                    Id = id,
                    Alias = "ztimage",
                    Label = string.Empty,
                    Memo = string.Empty,
                    BaseService = string.Empty,
                    BaseUri = baseuri,
                    ServerDirectory = new ACMESharp.AcmeServerDirectory()
                };
                vlt.SaveVault(v);
            }
            return id;
        }

        const string Domain = "centi.ztimage.com";
        const string WebPath = "E:\\WebSite\\centi.ztimage.com";


        static void Main(string[] args)
        {
            Guid id = Initialize();
            ACMESharp.AcmeRegistration registration = ACMESharpUtils.NewRegistration("", new string[] { "mailto:asmrobot@hotmail.com" }, true);
            ACMESharp.AuthorizationState state = ACMESharpUtils.NewIdentifier("dns1", Domain);

            ACMESharp.AuthorizationState completeState = ACMESharpUtils.CompleteChallenge("dns1", "http-01", "manual");

            var challenge = completeState.Challenges.First<ACMESharp.AuthorizeChallenge>(item => item.Type == "http-01");
            if (challenge == null)
            {
                Console.WriteLine("失败");
                return;
            }
            ACMESharp.ACME.HttpChallenge httpChallenge=challenge.Challenge as ACMESharp.ACME.HttpChallenge;
            if (httpChallenge == null)
            {
                Console.WriteLine("失败");
                return;
            }

            string savePath = Path.Combine(WebPath, httpChallenge.FilePath);
            string dir = savePath.Substring(0, savePath.LastIndexOf('/'));
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using (var file = File.OpenWrite(savePath))
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(httpChallenge.FileContent);
                file.Write(data, 0, data.Length);
            }

            ACMESharp.AuthorizationState submitState=ACMESharpUtils.SubmitChallenge("dns1", "http-01");
            
            while (true)
            {
                ACMESharp.AuthorizationState identifierState = ACMESharpUtils.UpdateIdentifier("dns1", "http-01");
                //using (var stream = File.Open("e:\\log.txt",FileMode.Append))
                //{
                //    identifierState.Save(stream);
                //}

                var subResultState = identifierState.Challenges.First<ACMESharp.AuthorizeChallenge>(item => item.Type == "http-01");
                if (subResultState == null)
                {
                    Console.WriteLine("失败");
                    return;
                }

                if (subResultState.Status.Equals("valid",StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("success");
                    break;
                }
                System.Threading.Thread.Sleep(30000);
            }

            ACMESharpUtils.NewCertificate("cert1", "dns1",null);

            ACMESharpUtils.SubmitCertificate("cert1");

            CertificateInfo info=ACMESharpUtils.UpdateCertificate("cert1");

            ACMESharpUtils.GetCertificate("cert1", "E:\\a.pem", "e:\\b.csr.pem");

            Console.WriteLine("ok");
            //using (var file = File.OpenWrite("d:\\state.log"))
            //{
            //    completeState.Save(file);
            //}
            Console.ReadKey();
        }

        
    }
}
