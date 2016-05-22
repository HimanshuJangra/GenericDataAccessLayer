using DalCore.DatabaseAccess;
using DalCore.SingleItemProcessor;
using FastMember;
using NProxy.Core;
using SharedComponents;
using SharedComponents.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Localization = L10n.Properties.Resources;

namespace DalCore.Repository
{
    public static class RepositoryOperator
    {
        private class StoredProcedureHandler : IInvocationHandler
        {
            /// <summary>
            /// Basic DAL Operator that help us handle commands
            /// </summary>
            protected IBasicDbAccess _accessLayer = new BasicDbAccess();

            /// <summary>
            /// List of parameters that contains it value too
            /// </summary>
            protected Dictionary<ParameterInfo, object> _parameters;

            /// <summary>
            /// list of output parameters that should write to "out"
            /// </summary>
            protected Dictionary<ParameterInfo, IDataParameter> _outputs;

            /// <summary>
            /// Name of Stored Procedure
            /// </summary>
            protected string _commandText;

            /// <summary>
            /// Return Type
            /// </summary>
            protected Type _returnType;

            /// <summary>
            /// Initialize some vital properties for SP execution
            /// </summary>
            /// <param name="target">Target Object</param>
            /// <param name="methodInfo">Method Information</param>
            /// <param name="parameters">Parameters from execution code</param>
            /// <returns></returns>
            protected virtual object PrepareInitialoization(object target, MethodInfo methodInfo, object[] parameters)
            {
                this._returnType = methodInfo.ReturnType;
                // fill parameter informations
                this._parameters = new Dictionary<ParameterInfo, object>();
                int index = 0;
                foreach (var item in methodInfo.GetParameters())
                {
                    this._parameters.Add(item, parameters[index++]);
                }

                this._commandText = methodInfo.Name;

                return Activator.CreateInstance(this._returnType);
            }

            public object Invoke(object target, MethodInfo methodInfo, object[] parameters)
            {
                var result = PrepareInitialoization(target, methodInfo, parameters);

                this._accessLayer.Execute(this.Execute, result, null);

                foreach (var item in _outputs)
                {
                    parameters[item.Key.Position] = item.Value.Value;
                }

                return result;
            }

            protected void PrepareExecute(IDbCommand command)
            {
                _outputs = new Dictionary<ParameterInfo, IDataParameter>();
                Type refString = Type.GetType("System.String&");
                // fill the parameters
                foreach (var item in this._parameters)
                {
                    ParameterInfo pi = item.Key;
                    ParameterDirection direction = ParameterDirection.Input;
                    if (pi.IsOut == true && pi.IsIn == false)
                        direction = ParameterDirection.Output;
                    else if (pi.IsOut == true && pi.IsIn == true)
                        direction = ParameterDirection.InputOutput;

                    var parameter = command.AddParameter($"@{pi.Name}", item.Value, direction, size: pi.ParameterType.In(typeof(string), refString) ? new Nullable<int>(-1) : null);

                    if (pi.IsOut == true)
                    {
                        _outputs.Add(pi, parameter);
                    }

                }
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = this._commandText;
                command.Prepare();
            }

            private void Execute(object transit, IDbCommand command)
            {
                if ((transit is ICollection) == false)
                {
                    throw new ArgumentException(nameof(Localization.DA001), nameof(transit));
                }
                var items = transit as IList;
                var accessor = TypeAccessor.Create(_returnType.GenericTypeArguments[0]);
                PrepareExecute(command);
                using (var reader = command.ExecuteReader())
                {
                    int columns = reader.FieldCount;
                    foreach (var record in reader.ToRecord())
                    {
                        var item = accessor.CreateNew();
                        items.Add(item);
                        for (int i = 0; i < columns; i++)
                        {
                            string name = record.GetName(i);
                            accessor[item, name] = record.GetValue(i);
                        }
                    }
                }
            }
        }
        private static ProxyFactory factory = new ProxyFactory();

        public static TRepository ToList<TRepository>()
            where TRepository : class, IRepository
        {
            return factory.CreateProxy<TRepository>(Type.EmptyTypes, new StoredProcedureHandler());
        }
    }
}
