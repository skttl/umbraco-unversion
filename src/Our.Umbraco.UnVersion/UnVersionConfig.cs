using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;


namespace Our.Umbraco.UnVersion
{
    public class UnVersionConfig : IUnVersionConfig
    {
        //private readonly static ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IDictionary<string, List<UnVersionConfigEntry>> ConfigEntries { get; set; }

        public UnVersionConfig(string configPath)
        {
            ConfigEntries = new Dictionary<string, List<UnVersionConfigEntry>>();

            LoadXmlConfig(configPath);
        }

        private void LoadXmlConfig(string configPath)
        {
            if (!File.Exists(configPath))
            {
                //TODO: Use Umbraco logger
                //Logger.Warn("Couldn't find config file " + configPath);
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
                            : "$_ALL" //TODO: Move to constant
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