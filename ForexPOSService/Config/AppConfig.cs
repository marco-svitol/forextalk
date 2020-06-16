using System;
using System.Configuration;

namespace ForexPOSService
{
    internal static class AppConfig
    {
        private static string _APIHelperBaseAddress;
        private static string _QueueListenerAddress;
        private static int    _QueueListenerPort;
        private static string _QueueDBConnString;
        private static string _ForexDBConnString;
        private static double _PullConsoleActionInterval;
        private static double _FetchForexTransactionsInterval;

        public static void Initialize()
        {
            try
            {
                _QueueListenerAddress = ConfigurationManager.AppSettings.Get("QueueListenerAddress");
                _QueueListenerPort = int.Parse(ConfigurationManager.AppSettings.Get("QueueListenerPort"));
                _QueueDBConnString = ConfigurationManager.ConnectionStrings["QueueDB"].ConnectionString;
                _ForexDBConnString = ConfigurationManager.ConnectionStrings["ForexDB"].ConnectionString;
#if DEBUG
                _QueueDBConnString = _QueueDBConnString.Replace("|ForexPOSService_Data|", ".\\Data");
                _APIHelperBaseAddress = "http://127.0.0.1:8080";
                _PullConsoleActionInterval = 5000;
                _FetchForexTransactionsInterval = 8000;
#else
                _QueueDBConnString = _QueueDBConnString.Replace("|ForexPOSService_Data|", $"{Environment.GetEnvironmentVariable("PROGRAMDATA")}\\ForexPOSService\\Data");
                _APIHelperBaseAddress = ConfigurationManager.AppSettings.Get("APIHelperBaseAddress");
                _PullConsoleActionInterval = double.Parse(ConfigurationManager.AppSettings.Get("PullConsoleActionInterval"));
                _FetchForexTransactionsInterval = double.Parse(ConfigurationManager.AppSettings.Get("FetchForexTransactionsInterval"));
#endif
            }
            catch (Exception ex) {
                throw new ConfigurationErrorsException("Error while initializing config variables", ex);
            }
        }

        public static string APIHelperBaseAddress()
        {
            return _APIHelperBaseAddress;
        }
       
        public static string QueueListenerAddress()
        {
            return _QueueListenerAddress;
        }
        public static int QueueListenerPort()
        {
            return _QueueListenerPort;
        }
        public static string QueueDBConnString()
        {
            return _QueueDBConnString;
        }
        public static string ForexDBConnString()
        {
            return _ForexDBConnString;
        }
        public static double PullConsoleActionInterval()
        {
            return _PullConsoleActionInterval;
        }
        public static double FetchForexTransactionsInterval()
        {
            return _FetchForexTransactionsInterval;
        }
    }


}
