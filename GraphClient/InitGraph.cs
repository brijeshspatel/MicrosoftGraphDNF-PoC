using Microsoft.Extensions.Configuration;
using NLog;
using System;

namespace CBA.SOE.ConsoleApp
{
    public static class InitGraph
    {
        // Logger object
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static GraphApi.Entities.Settings LoadGraphSettings(IConfiguration config)
        {
            // Load settings from configuration
            var settings = config.GetRequiredSection("Settings").Get<GraphApi.Entities.Settings>() ?? throw new Exception("Could not load app settings.");

            // Retrieve the secret key from environment variables
            var secretKey = Environment.GetEnvironmentVariable("ClientSecret");
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("ClientSecret environment variable is not set.");
            }

            // Set the secret key in the settings
            settings.ClientSecret = secretKey;

            return settings;
        }

        public static GraphApi.Entities.Settings LoadGraphSettings()
        {
            // Load settings from configuration
            var settings = new GraphApi.Entities.Settings
            {
                ClientId = System.Configuration.ConfigurationManager.AppSettings["ClientId"],
                TenantId = System.Configuration.ConfigurationManager.AppSettings["TenantId"]
            };

            // Retrieve the secret key from environment variables
            var secretKey = Environment.GetEnvironmentVariable("MyGraphApiClientSecret");
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("MyGraphApiClientSecret environment variable is not set.");
            }

            // Set the secret key in the settings
            settings.ClientSecret = secretKey;

            return settings;
        }
        public static void InitializeGraph()
        {
            try
            {
                // Initialize Graph API with app-only authentication
                GraphApi.GraphHelper.InitializeGraphForAppOnlyAuth(LoadGraphSettings());
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[InitializeGraph] Error initializing Graph: {ex.Message}");
            }
        }
    }
}
