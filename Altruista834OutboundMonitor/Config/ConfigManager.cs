using System;
using System.IO;
using Newtonsoft.Json;

namespace Altruista834OutboundMonitor.Config
{
    public static class ConfigManager
    {
        public static AppConfig Load(string basePath)
        {
            var configPath = Path.Combine(basePath, "Config", "config.json");
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("config.json not found", configPath);
            }

            var raw = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<AppConfig>(raw);
            if (config == null)
            {
                throw new InvalidOperationException("Unable to parse config.json");
            }

            Validate(config);
            return config;
        }

        private static void Validate(AppConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Folders.VendorExtractUtility) ||
                string.IsNullOrWhiteSpace(config.Folders.Proprietary) ||
                string.IsNullOrWhiteSpace(config.Folders.Hold) ||
                string.IsNullOrWhiteSpace(config.Folders.Drop))
            {
                throw new InvalidOperationException("Folder paths are mandatory.");
            }

            if (config.Monitoring.ExpectedFiles.Count == 0)
            {
                throw new InvalidOperationException("Expected file list cannot be empty.");
            }
        }
    }
}
