using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using GenericDataAccessLayer.Core.DatabaseAccess;
using GenericDataAccessLayer.Core.SingleItemProcessor;
using Localization = L10n.Properties.Resources;
using SharedComponents;

namespace GenericDataAccessLayer.Core
{
    /// <summary>
    /// Allow Import all existing Data Access Layers
    /// </summary>
    public class FastAccess : IDisposable
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

        #endregion

        /// <summary>
        /// Initialize new Catalog of all DALs
        /// </summary>
        private FastAccess()
        {
            this.Initialize();
        }

        private void Initialize()
        {
            var catalogs = new List<ComposablePartCatalog>();
            var config = DalCoreConfiguration.Instance;
            if (string.IsNullOrEmpty(config.Directory) == false)
            {
                catalogs.Add(new DirectoryCatalog(config.Directory));
            }
            if (string.IsNullOrEmpty(config.Assembly) == false)
            {
                catalogs.Add(new AssemblyCatalog(config.Assembly));
            }

            if (catalogs.Count == 0)
            {
                throw new IndexOutOfRangeException(nameof(Localization.GE001));
            }

            var catalog = new AggregateCatalog(catalogs);
            this._container = new CompositionContainer(catalog, true);
            this._container.ComposeParts(this);
        }
        /// <summary>
        /// MEF Container. Only for Dispose
        /// </summary>
        private CompositionContainer _container;

        /// <summary>
        /// Load default implementation for logger
        /// </summary>
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<Lazy<ILogger>> DefaultLoggers;

        /// <summary>
        /// Get Data Access Layer as Interface
        /// </summary>
        /// <typeparam name="T">Transfer Object </typeparam>
        /// <returns></returns>
        public IDataAccessLayer GetDal<T>()
            where T : SharedComponents.Data.IDataTransferObject
        {
            return this.GetDal(typeof(T));
        }

        public IDataAccessLayer GetDal(Type dtoType, bool throwOnNull = true)
        {
            var dal = this._plugins?.FirstOrDefault(a => a.Metadata.ParameterType == dtoType)?.Value;
            if (dal == null && throwOnNull == true)
            {
                throw new ArgumentNullException(dtoType.Name, nameof(Localization.GE002));
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
        [ImportMany(AllowRecomposition = true)]
        private IEnumerable<Lazy<IDataAccessLayer, ITypeAccessor>> _plugins;

        #region dispose
        /// <summary>
        /// Dispose managed and unmanaged resources
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Manage Disposing
        /// </summary>
        /// <param name="disposing">if true, dispose managed </param>
        private void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                this._container?.ReleaseExports(this._plugins);
                this._container?.ReleaseExports(this.DefaultLoggers);
            }
        }
        /// <summary>
        /// Dispose unmanaged resources
        /// </summary>
        ~FastAccess()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
