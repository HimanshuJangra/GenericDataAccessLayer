using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using SharedDal.DatabaseAccess;
using SharedDal.SingleItemProcessor;
using Localization = L10n.Properties.Resources;

namespace SharedDal
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
                throw new IndexOutOfRangeException(Localization.FastAccessNoCatalogSelected);
            }

            var catalog = new AggregateCatalog(catalogs);
            var container = new CompositionContainer(catalog, true);
            container.ComposeParts(this);
        }
        /// <summary>
        /// Get Data Access Layer as Interface
        /// </summary>
        /// <typeparam name="T">Transfer Object </typeparam>
        /// <returns></returns>
        public IDataAccessLayer GetDal<T>()
            where T : SharedComponents.Data.IDataTransferObject
        {
            return this._plugins?.FirstOrDefault(a => a.Metadata.ParameterType == typeof(T))?.Value;
        }

        /// <summary>
        /// Get Data Access Layer already casted to a specific Type
        /// </summary>
        /// <typeparam name="TDto"></typeparam>
        /// <typeparam name="TDal"></typeparam>
        /// <returns></returns>
        public TDal GetConcreteDal<TDal>()
            where TDal : IDataAccessLayer
        {
            return (TDal)this._plugins.FirstOrDefault(a => a.Metadata.ConcreteType == typeof(TDal))?.Value;
        }

        [ImportMany]
        private IEnumerable<Lazy<IDataAccessLayer, ITypeAccessor>> _plugins;
    }
}
