using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Serilog;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Diagnostics;

namespace ForexPOSService
{
    public static class Utils
    {
        public readonly static string POSId = CalcPOSId();

        private static string CalcPOSId()
        {
            string posid = MD5Hash($"{GetBIOSProperty("SerialNumber")}{GetBIOSProperty("Manufacturer")}");
            Log.Verbose($"CalcPOSId MD5 is {posid} with sn:{GetBIOSProperty("SerialNumber")},vn:{GetBIOSProperty("Manufacturer")}");
            return posid;
        }

        private static string GetBIOSProperty(string field)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            string collectedInfo = ""; // here we will put the informa

            searcher.Query = new ObjectQuery("select * from Win32_BIOS");
            foreach (ManagementObject share in searcher.Get())
            {
                //then, the serial number of BIOS
                collectedInfo += share.GetPropertyValue(field).ToString();
            }
            searcher.Dispose();
            return collectedInfo;
        }

        private static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            md5provider.Dispose();
            return hash.ToString();
        }

        public static bool MySQLIsRunning()
        {
            Process[] pname = Process.GetProcessesByName("mysqld");
            if (pname.Length > 0) return true; else return false;
            /*try
            {  
                ServiceController sc = new ServiceController("mysqld");
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;*/
        }
    }
}
