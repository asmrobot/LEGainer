using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LEGainer.Utils;
using ACMESharp.Vault.Model;
using System.IO;
using ZTImage.Log;
using ACMESharp;

namespace LEGainer
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.EnableConsole();
            Trace.EnableFile();

            if (!CheckParmeter())
            {
                return;
            }
            Trace.Info("check config ok!~");

            InitializeVault();
            Trace.Info("init vault ok!~");

            try
            {
                ACMESharpUtils.NewRegistration("", new string[] { "mailto:" + Config.Mail }, true);
            }
            catch(Exception ex)
            {
                Trace.Error("registration error", ex);
                return;
            }
            Trace.Info("registration ok!~");

            try
            {
                ACMESharpUtils.NewIdentifier("dns1", Config.Domain);
            }
            catch (Exception ex)
            {
                Trace.Error("newidentityfier error", ex);
                return;
            }
            Trace.Info("newidentityfier ok!~");

            try
            {
                AuthorizationState state = ACMESharpUtils.CompleteChallenge("dns1", "http-01", "manual");
                if (!CreateChallengeFile(state))
                {
                    Trace.Error("create challenge file erro");
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.Error("complete challenge error",ex);
                return;
            }
            Trace.Info("challege ok");

            try
            {
                ACMESharpUtils.SubmitChallenge("dns1", "http-01");
            }
            catch (Exception ex)
            {
                Trace.Error("submit challenge error",ex);
                return;
            }
            Trace.Info("submit challage ok!~");

            Trace.Info("wait LE identifier");
            DateTime startT = DateTime.Now;
            bool result = false;
            while ((DateTime.Now-startT).TotalSeconds<300)
            {
                AuthorizationState state = null;
                try
                {
                    state = ACMESharpUtils.UpdateIdentifier("dns1", "http-01");
                }
                catch(Exception ex)
                {
                    Trace.Error("update identifier error");
                    return;
                }
                if (state == null)
                {
                    Trace.Error("update identifier state is null");
                    return;
                }
                
                var subResultState = state.Challenges.First<ACMESharp.AuthorizeChallenge>(item => item.Type == "http-01");
                if (subResultState == null)
                {
                    SaveState(state);
                    Trace.Error("state is null");
                    return;
                }

                if (subResultState.Status.Equals("valid", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = true;
                    break;
                }
                else if (subResultState.Status.Equals("invalid", StringComparison.CurrentCultureIgnoreCase))
                {
                    SaveState(state);
                    Trace.Error("state is invalid");
                    return;
                }
                else
                {
                    Trace.Info(DateTime.Now.ToString("HH:mm:ss") + ",status is:" + subResultState.Status);
                }
                System.Threading.Thread.Sleep(5000);
            }
            if (!result)
            {
                Trace.Error("update identifer timeout");
                return;
            }
            Trace.Info("update identifier ok!~");

            try
            {
                ACMESharpUtils.NewCertificate("cert1", "dns1", null);
            }
            catch(Exception ex)
            {
                Trace.Error("new certificate erro",ex);
                return;
            }
            Trace.Info("new certificate is ok!~");

            try
            {
                ACMESharpUtils.SubmitCertificate("cert1");
            }
            catch (Exception ex)
            {
                Trace.Error("submit certificateerro",ex);
                return;
            }
            Trace.Info("submit certificate is ok!~");

            try
            {
                CertificateInfo info = ACMESharpUtils.UpdateCertificate("cert1");
            }
            catch(Exception ex)
            {
                Trace.Error("update certificate erro",ex);
                return;
            }
            Trace.Info("update certificate is ok!~");

            if (!GenericCertificate())
            {
                return;
            }
            
            Trace.Info("success!~");
            if (Environment.UserInteractive)
            {
                Trace.Info("Enter press any key exit!~");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns></returns>
        private static bool CheckParmeter()
        {
            if (string.IsNullOrEmpty(Config.Mail))
            {
                Trace.Error("mail is not null!~");
                return false;
            }

            if (string.IsNullOrEmpty(Config.Domain))
            {
                Trace.Error("domain is null");
                return false;
            }

            if (string.IsNullOrEmpty(Config.WebDir))
            {
                Trace.Error("webdir is null");
                return false;
            }

            if (!Directory.Exists(Config.WebDir))
            {
                Trace.Error("webdir is not exists");
                return false;
            }

            if (string.IsNullOrEmpty(Config.CertificateSaveDir))
            {
                Trace.Error("certificate save dir is null");
                return false;
            }
            if (!Directory.Exists(Config.CertificateSaveDir))
            {
                Trace.Error("certificate save dir is not exists");
                return false;
            }
            return true; 
        }
        
        /// <summary>
        /// 初始化vault
        /// </summary>
        public static void InitializeVault()
        {
            string baseuri = ACMESharpUtils.WELL_KNOWN_BASE_SERVICES[ACMESharpUtils.WELL_KNOWN_LE];
            using (var vlt = ACMESharpUtils.GetVault())
            {
                vlt.InitStorage(true);
                var v = new VaultInfo
                {
                    Id = ACMESharp.Vault.Util.EntityHelper.NewId(),
                    Alias = "ztimage",
                    Label = string.Empty,
                    Memo = string.Empty,
                    BaseService = string.Empty,
                    BaseUri = baseuri,
                    ServerDirectory = new ACMESharp.AcmeServerDirectory()
                };
                vlt.SaveVault(v);
            }
        }

        /// <summary>
        /// 创建站点验证文件
        /// </summary>
        /// <returns></returns>
        private static bool CreateChallengeFile(AuthorizationState state)
        {

            var challenge = state.Challenges.First<ACMESharp.AuthorizeChallenge>(item => item.Type == "http-01");
            if (challenge == null)
            {
                Trace.Error("不存在 http-01节点");
                return false;
            }

            ACMESharp.ACME.HttpChallenge httpChallenge = challenge.Challenge as ACMESharp.ACME.HttpChallenge;
            if (httpChallenge == null)
            {
                Trace.Error("不存在http-01 内容节点");
                return false;
            }

            string savePath = Path.Combine(Config.WebDir, httpChallenge.FilePath);
            
            string dir = savePath.Substring(0, savePath.LastIndexOfAny(new char[] { '/' ,'\\'}));
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                using (var file = File.OpenWrite(savePath))
                {
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(httpChallenge.FileContent);
                    file.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Trace.Error("创建站点验证文件失败", ex);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 生成证书
        /// </summary>
        /// <returns></returns>
        private static bool GenericCertificate()
        {
            string savePathPrefix = Config.CertificateSaveDir;
            if (!savePathPrefix.EndsWith("\\") && !savePathPrefix.EndsWith("/"))
            {
                savePathPrefix += Path.DirectorySeparatorChar + Config.Domain + "_";
            }
            try
            {
                ACMESharpUtils.GetCertificate("cert1", savePathPrefix+"key.pem", savePathPrefix+"csr.pem", savePathPrefix+"certificate.pem", savePathPrefix+"certificate.der", savePathPrefix+"issuer.pem", savePathPrefix+"issuer.der", savePathPrefix+"pkcs12.pfx", Config.PFXPassword,overwrite:true);
                string chinapem = savePathPrefix + "chain.pem";
                if (File.Exists(chinapem))
                {
                    File.Delete(chinapem);
                }
                string content = string.Empty;
                content = File.ReadAllText(savePathPrefix + "certificate.pem")+"\r\n";
                content += File.ReadAllText(savePathPrefix + "issuer.pem");
                File.WriteAllText(chinapem, content);
            }
            catch(Exception ex)
            {
                Trace.Error("生成证书失败!~", ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 保存状态
        /// </summary>
        /// <param name="state"></param>
        private static void SaveState(AuthorizationState state)
        {
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            using (var stream = File.OpenWrite(logFile))
            {
                state.Save(stream);
            }
        }
        
    }
}
