namespace Ocelot.DependencyInjection
{
    using Configuration.File;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class ConfigurationBuilderExtensions
    {
        [Obsolete("Please set BaseUrl in ocelot.json GlobalConfiguration.BaseUrl")]
        public static IConfigurationBuilder AddOcelotBaseUrl(this IConfigurationBuilder builder, string baseUrl)
        {
            var memorySource = new MemoryConfigurationSource
            {
                InitialData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("BaseUrl", baseUrl)
                }
            };

            builder.Add(memorySource);

            return builder;
        }

        public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, IHostingEnvironment env)
        {
            return builder.AddOcelot(".", env);
        }

        public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, string folder, IHostingEnvironment env)
        {
            const string primaryConfigFile = "ocelot.json";

            const string globalConfigFile = "ocelot.global.json";

            string subConfigPattern = $@"^ocelot\.([a-zA-Z0-9]+|[a-zA-Z0-9]+\.{env?.EnvironmentName})\.json$";

            var reg = new Regex(subConfigPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var files = new DirectoryInfo(folder)
                .EnumerateFiles()
                .Where(fi => reg.IsMatch(fi.Name))
                .ToList();

            var fileConfiguration = new FileConfiguration();

            foreach (var file in files)
            {
                if (files.Count > 1 && file.Name.Equals(primaryConfigFile, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var lines = File.ReadAllText(file.FullName);

                var config = JsonConvert.DeserializeObject<FileConfiguration>(lines);

                if (file.Name.Equals(globalConfigFile, StringComparison.OrdinalIgnoreCase))
                {
                    fileConfiguration.GlobalConfiguration = config.GlobalConfiguration;
                }

                fileConfiguration.Aggregates.AddRange(config.Aggregates);
                fileConfiguration.ReRoutes.AddRange(config.ReRoutes);
            }

            var json = JsonConvert.SerializeObject(fileConfiguration);

            File.WriteAllText(primaryConfigFile, json);

            builder.AddJsonFile(primaryConfigFile, false, false);

            return builder;
        }
    }
}
