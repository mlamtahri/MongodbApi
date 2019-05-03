using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Fluentd;

namespace Foundation.ObjectService.WebUI
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static int Main(string[] args)
        {
            var fluentHost = Common.GetConfigurationVariable(Configuration, "OBJECT_FLUENTD_HOST", "Logging:FluentD:Host", "localhost");
            var fluentPortStr = Common.GetConfigurationVariable(Configuration, "OBJECT_FLUENTD_HOST", "Logging:FluentD:Port", "24224");
            var fluentPrefix = Common.GetConfigurationVariable(Configuration, "OBJECT_FLUENTD_PREFIX", "Logging:FluentD:Prefix", "fdns.object");
            int.TryParse(fluentPortStr, out int fluentPort);

            FluentdSinkOptions options = new FluentdSinkOptions(fluentHost, fluentPort);
            options.Tag = fluentPrefix;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Fluentd(options)
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateWebHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseConfiguration(Configuration)
                .UseSerilog();
    }
#pragma warning restore 1591
}
