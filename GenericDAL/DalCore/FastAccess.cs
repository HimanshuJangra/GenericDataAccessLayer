using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using DalCore.DatabaseAccess;
using DalCore.SingleItemProcessor;
using Localization = L10n.Properties.Resources;
using SharedComponents;

namespace DalCore
{
    /// <summary>
    /// Allow Import all existing Data Access Layers
    /// </summary>
    public class FastAccess
    {
        #region Singleton

        private static readonly Object Sync = new Object();
        private static FastAccess _instance;
        public static FastAccess Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Sync)
                    {
                        if (_instance == null)
                        {
                            _instance = new FastAccess();
                        }
                    }
                }
                return _instance;
            }
        }

        public static FastAccess InitializeWithArguments(String importDirectory, IEnumerable<Assembly> assemblies = null)
        {
            lock (Sync)
            {
                Instance.DictionaryCatalogPath = importDirectory;
                Instance.Assemblies = assemblies;
                Instance.Initialize();
            }

            return Instance;
        }

        #endregion

        /// <summary>
        /// Initialize new Catalog of all DALs
        /// </summary>
        private FastAccess()
        {
        }

        public String DictionaryCatalogPath { get; set; } = ".";
        public IEnumerable<Assembly> Assemblies { get; set; }

        public void Initialize()
        {
            var catalogs = new List<ComposablePartCatalog>();
            if (string.IsNullOrEmpty(this.DictionaryCatalogPath) == false)
            {
                catalogs.Add(new DirectoryCatalog(this.DictionaryCatalogPath));
            }
            if (this.Assemblies != null)
            {
                catalogs.AddRange(Assemblies.Select(a => new AssemblyCatalog(a)));
            }

            if (catalogs.Count == 0)
            {
                throw new IndexOutOfRangeException(nameof(Localization.GE001));
            }

            var catalog = new AggregateCatalog(catalogs);
            var container = new CompositionContainer(catalog, true);
            container.ComposeParts(this);
        }

        /// <summary>
        /// Load default implementation for logger
        /// </summary>
        [ImportMany] public IEnumerable<ILogger> DefaultLoggers;

        /// <summary>
        /// Get Data Access Layer as Interface
        /// </summary>
        /// <typeparam name="T">Transfer Object </typeparam>
        /// <returns></returns>
        public IDataAccessLayer GetDal<T>()
            where T : SharedComponents.Data.IDataTransferObject
        {
            var dal = this._plugins?.FirstOrDefault(a => a.Metadata.ParameterType == typeof(T))?.Value;
            if (dal == null)
            {
                throw new ArgumentNullException(typeof(T).Name, nameof(Localization.GE002));
            }
            return dal;
        }

        /// <summary>
        /// Get Data Access Layer already casted to a specific Type
        /// </summary>
        /// <typeparam name="TDal">Concrete Data Access Layer Definition</typeparam>
        /// <returns></returns>
        public TDal GetConcreteDal<TDal>()
            where TDal : IDataAccessLayer
        {
            var dal = (TDal)this._plugins.FirstOrDefault(a => a.Metadata.ConcreteType == typeof(TDal))?.Value;
            if (dal == null)
            {
                throw new ArgumentNullException(typeof(TDal).Name, nameof(Localization.GE002));
            }
            return dal;
        }
        /// <summary>
        /// load all data access layers
        /// </summary>
        [ImportMany]
        private IEnumerable<Lazy<IDataAccessLayer, ITypeAccessor>> _plugins;
    }
}
