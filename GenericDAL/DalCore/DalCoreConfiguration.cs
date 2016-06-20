using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Reflection;

namespace GenericDataAccessLayer.Core
{
    public class DalCoreConfiguration : ConfigurationSection
    {
        [ConfigurationProperty(nameof(Directory), DefaultValue = null, IsRequired = false, IsKey = false)]
        public String Directory
        {
            get
            {
                return (String)this[nameof(this.Directory)];
            }
            set
            {
                this[nameof(this.Directory)] = value;
            }
        }
        [ConfigurationProperty(nameof(Assembly), DefaultValue = null, IsRequired = false, IsKey = false)]
        public String Assembly
        {
            get
            {
                return (String)this[nameof(this.Assembly)];
            }
            set
            {
                this[nameof(this.Assembly)] = value;
            }
        }

        public void Save()
        {
            _config.Save(ConfigurationSaveMode.Minimal);
        }

        private DalCoreConfiguration() { }

        private static readonly Object Sync = new Object();
        private static DalCoreConfiguration _instance;
        private static Configuration _config;
        public static DalCoreConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Sync)
                    {
                        if (_instance == null)
                        {
                            _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                            _instance = _config.GetSection(nameof(DalCoreConfiguration)) as DalCoreConfiguration;
                        }
                    }
                }
                return _instance;
            }
        }
    }
}

