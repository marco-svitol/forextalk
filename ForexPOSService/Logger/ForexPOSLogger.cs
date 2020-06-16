using Serilog;

namespace ForexPOSService
{
    public static class ForexPOSLogger
    { 
        public static void InitializeLogger()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
#else
                 
                .ReadFrom.AppSettings()
#endif
                .WriteTo.File(
                    path: ".\\logs\\ForexPOSService.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    rollOnFileSizeLimit: true, fileSizeLimitBytes: 1048576
                    )
                .WriteTo.Console()
                .CreateLogger();
        }
    }

}

