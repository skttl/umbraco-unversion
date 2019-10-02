using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using Umbraco.Core.Logging;

namespace Our.Umbraco.UnVersion
{
    public class UnVersionConfig : IUnVersionConfig
    {
        private readonly ILogger _logger;

        public IDictionary<string, List<UnVersionConfigEntry>> ConfigEntries { get; set; }

        public UnVersionConfig(ILogger logger)
        {
            _logger = logger;
            ConfigEntries = new Dictionary<string, List<UnVersionConfigEntry>>();

            var appPath = HttpRuntime.AppDomainAppPath;
            var configFilePath = Path.Combine(appPath, @"config\unVersion.config");

            LoadXmlConfig(configFilePath);
        }

        private void LoadXmlConfig(string configPath)
        {
            if (!File.Exists(configPath))
            {
                _logger.Warn<UnVersionConfig>("Couldn't find config file " + configPath);
                return;
            }

            var xmlConfig = new XmlDocument();
            xmlConfig.Load(configPath);

            foreach (XmlNode xmlConfigEntry in xmlConfig.SelectNodes("/unVersionConfig/add"))
            {
                if (xmlConfigEntry.NodeType == XmlNodeType.Element)
                {
                    var configEntry = new UnVersionConfigEntry
                    {
                        DocTypeAlias = xmlConfigEntry.Attributes["docTypeAlias"] != null
                            ? xmlConfigEntry.Attributes["docTypeAlias"].Value
                            : "$_ALL"
                    };

                    if (xmlConfigEntry.Attributes["rootXpath"] != null)
                        configEntry.RootXPath = xmlConfigEntry.Attributes["rootXpath"].Value;

                    if (xmlConfigEntry.Attributes["maxDays"] != null)
                        configEntry.MaxDays = Convert.ToInt32(xmlConfigEntry.Attributes["maxDays"].Value);

                    if (xmlConfigEntry.Attributes["maxCount"] != null)
                        configEntry.MaxCount = Convert.ToInt32(xmlConfigEntry.Attributes["maxCount"].Value);

                    if (!ConfigEntries.ContainsKey(configEntry.DocTypeAlias))
                        ConfigEntries.Add(configEntry.DocTypeAlias, new List<UnVersionConfigEntry>());

                    ConfigEntries[configEntry.DocTypeAlias].Add(configEntry);
                }
            }
        }
    }

    public class UnVersionConfigEntry
    {
        public UnVersionConfigEntry()
        {
            MaxDays = MaxCount = int.MaxValue;
        }

        public string DocTypeAlias { get; set; }
        public string RootXPath { get; set; }
        public int MaxDays { get; set; }
        public int MaxCount { get; set; }
    }

    public interface IUnVersionConfig
    {
        IDictionary<string, List<UnVersionConfigEntry>> ConfigEntries { get; }
    }
}