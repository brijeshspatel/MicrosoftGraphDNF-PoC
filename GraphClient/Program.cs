using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace CBA.SOE.ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var services = ConfigureServices();

            using (ServiceProvider serviceProvider = services.BuildServiceProvider(true))
            {
                var logger = serviceProvider.GetService<ILogger<Program>>();

                try
                {
                    logger?.LogInformation("Program started");
                    await serviceProvider?.GetService<Application>()?.Run(args);
                }
                catch (Exception ex)
                {
                    //Catch setup errors
                    logger?.LogError(ex, "Stopped program because of exception");
                    Console.WriteLine(ex);
                    throw;
                }
                finally
                {
                    logger?.LogInformation("Program ended");

                    // Ensure to flush and stop internal timers/threads before application exit
                    NLog.LogManager.Flush();
                    NLog.LogManager.Shutdown();
                }
            }
        }

        private static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            var config = LoadConfiguration();

            services.AddSingleton(config);
            services.AddTransient<Application>();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddNLog("NLog.config");
            });

            return services;
        }

        public static IConfiguration LoadConfiguration()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .Build();

            return config;
        }
    }
}
