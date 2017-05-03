using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace LEGainer
{
    public static class Config
    {
        public static string Mail
        {
            get
            {
                return Get("Mail");
            }
        }

        public static string Domain
        {
            get
            {
                return Get("Domain");
            }
        }

        public static string WebDir
        {
            get
            {
                return Get("WebDir");
            }
        }

        public static string CertificateSaveDir
        {
            get
            {
                return Get("CertificateSaveDir");
            }
        }

        public static string PFXPassword
        {
            get
            {
                return Get("PFXPassword");
            }
        }

        private static string Get(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        private static void Set(string key, string value)
        {
            ConfigurationManager.AppSettings.Set(key, value);
        }
    }
}
