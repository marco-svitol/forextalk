using Serilog;
using System;
using System.IO;
using System.Reflection;
using Topshelf;

namespace ForexPOSService
{
    class Program
    {
        static void Main(string[] args)
        {
            
            try
            {
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Directory.SetCurrentDirectory(path);
                ForexPOSLogger.InitializeLogger();
                AppConfig.Initialize();  //Load App config and catch any missign parameter
                Store.StoreInitialize();
                WatsonAPIListener poslistener = new WatsonAPIListener(); //Listen to Forex_Next (in)
                ApiHelper.InitializeClient(); //Initialize API caller for Console (out)
                ApiHelper.InitializeQueueClient();
                //ToDO: Test if self calling WatsonAPIListener replies or not!
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException;
                string stacktrace = ex.Message + ex.StackTrace;
                while (inner != null)
                {
                    stacktrace += inner.StackTrace;
                    inner = inner.InnerException;
                }
                Log.Fatal(stacktrace);
                Environment.Exit(1);
            }
            finally{

            }
            var exitCode = HostFactory.Run(x =>
            {
                x.SetStartTimeout(new TimeSpan(90));
                x.StartAutomaticallyDelayed();

                x.Service<MainService>(s =>
                {
                    s.ConstructUsing(mainservice => new MainService());
                    s.WhenStarted(mainservice => mainservice.Start());
                    s.WhenStopped(mainservice => mainservice.Stop());
                });

                x.StartAutomatically();
                x.RunAsLocalSystem();
                x.EnableServiceRecovery(rc =>
                {
                    // Has no corresponding setting in the Recovery dialogue.
                    // OnCrashOnly means the service will not restart if the application returns
                    // a non-zero exit code.  By convention, an exit code of zero means ‘success’.
                    rc.OnCrashOnly();
                    // Corresponds to ‘First failure: Restart the Service’
                    // Note: 0 minutes delay means restart immediately
                    rc.RestartService(delayInMinutes: 1);
                    // Corresponds to ‘Second failure: Restart the Service’
                    // Note: TopShelf will configure a 1 minute delay before this restart, but the
                    // Recovery dialogue only shows the first restart delay (0 minutes)
                    rc.RestartService(delayInMinutes: 0);
                    // Corresponds to ‘Subsequent failures: Restart the Service’
                    rc.TakeNoAction();
                    // Corresponds to ‘Reset fail count after: 1 days’
                    rc.SetResetPeriod(days: 1);
                });
                x.SetServiceName("ForexPOSService");
                x.SetDisplayName("ForexPOS Service");
                x.SetDescription("ForexPOS mantiene la sincronizzazione e gestisce lo scambio di messaggi tra la cassa e la console.");
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;

        }
    }
}
