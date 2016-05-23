using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using DalCore.DatabaseAccess;
using NProxy.Core;
using SharedComponents;
using Localization = L10n.Properties.Resources;

namespace DalCore.Repository.StoredProcedure
{
    /// <summary>
    /// Base defintion for Stored Procedure Handler
    /// </summary>
    public abstract class SpHandlerBase : IInvocationHandler
    {
        /// <summary>
        /// Basic DAL Operator that help us handle commands
        /// </summary>
        protected readonly IBasicDbAccess AccessLayer = new BasicDbAccess();

        /// <summary>
        /// List of parameters that contains it value too
        /// </summary>
        protected Dictionary<ParameterInfo, object> Parameters;

        /// <summary>
        /// list of output parameters that should write to "out"
        /// </summary>
        protected Dictionary<ParameterInfo, IDataParameter> Outputs;

        /// <summary>
        /// Name of Stored Procedure
        /// </summary>
        protected string CommandText;

        /// <summary>
        /// Return Type
        /// </summary>
        protected Type ReturnType;

        /// <summary>
        /// Initialize some vital properties for SP execution
        /// </summary>
        /// <param name="methodInfo">Method Information</param>
        /// <param name="parameters">Parameters from execution code</param>
        /// <returns></returns>
        protected virtual object PrepareInitialoization(MethodInfo methodInfo, object[] parameters)
        {
            ReturnType = methodInfo.ReturnType;
            // fill parameter informations
            Parameters = new Dictionary<ParameterInfo, object>();
            int index = 0;
            foreach (var item in methodInfo.GetParameters())
            {
                Parameters.Add(item, parameters[index++]);
            }

            CommandText = methodInfo.Name;

            return Activator.CreateInstance(ReturnType);
        }
        /// <summary>Processes an invocation on a target.</summary>
        /// <param name="target">The target object.</param>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="parameters">The parameter values.</param>
        /// <returns>The return value.</returns>
        public virtual object Invoke(object target, MethodInfo methodInfo, object[] parameters)
        {
            var result = PrepareInitialoization(methodInfo, parameters);

            AccessLayer.Execute(Execute, result, null);

            foreach (var item in Outputs)
            {
                parameters[item.Key.Position] = item.Value.Value;
            }

            return result;
        }
        /// <summary>
        /// Pre-Execution Process that allow create stored Procedure parameters
        /// </summary>
        /// <param name="command"></param>
        protected virtual void PrepareExecute(IDbCommand command)
        {
            Outputs = new Dictionary<ParameterInfo, IDataParameter>();
            Type refString = Type.GetType("System.String&");
            // fill the parameters
            foreach (var item in Parameters)
            {
                ParameterInfo pi = item.Key;
                ParameterDirection direction = ParameterDirection.Input;
                if (pi.IsOut == true && pi.IsIn == false)
                    direction = ParameterDirection.Output;
                else if (pi.IsOut == true && pi.IsIn == true)
                    direction = ParameterDirection.InputOutput;
                int? size = null;

                if (pi.ParameterType.In(typeof(string), refString))
                {
                    size = -1;
                }
                var parameter = command.AddParameter($"@{pi.Name}", item.Value, direction, size: size);

                if (pi.IsOut == true)
                {
                    Outputs.Add(pi, parameter);
                }
            }
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = CommandText;
            command.Prepare();
        }

        /// <summary>
        /// Execute definition
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        protected abstract void Execute(object transit, IDbCommand command);
    }
}
