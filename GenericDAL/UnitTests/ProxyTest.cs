using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Reflection;
using GenericDataAccessLayer.LazyDal.Attributes;
using NProxy.Core;
using System.Linq;

namespace UnitTests
{
    [TestClass()]
    public class ProxyTest
    {
        private int counts = int.MaxValue / 2048;
        public class Proxy : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
            }
        }

        public class NProxy : IInvocationHandler
        {
            /// <summary>Processes an invocation on a target.</summary>
            /// <param name="target">The target object.</param>
            /// <param name="methodInfo">The method information.</param>
            /// <param name="parameters">The parameter values.</param>
            /// <returns>The return value.</returns>
            public object Invoke(object target, MethodInfo methodInfo, object[] parameters)
            {
                return null;
            }
        }

        [TestMethod]
        public void MyTestMethod()
        {
            var w = Stopwatch.StartNew();
            var generator = new Castle.DynamicProxy.ProxyGenerator();
            var proxy = generator.CreateInterfaceProxyWithoutTarget<Repository.Calls.TestRepository>(new Proxy());
            w.Stop();
            Console.WriteLine(w.Elapsed);
            w.Reset();
            int counter = counts;
            w.Start();
            while (true)
            {
                proxy.Read();
                if (counter-- == 0)
                {
                    break;
                }
            }
            w.Stop();
            Console.WriteLine(w.Elapsed);
        }

        [TestMethod]
        public void MyTestMethod2()
        {
            var w = Stopwatch.StartNew();
            var proxy = new ProxyFactory().CreateProxy<Repository.Calls.TestRepository>(Type.EmptyTypes, new NProxy());
            w.Stop();
            Console.WriteLine(w.Elapsed);
            w.Reset();
            int counter = counts;
            w.Start();
            while (true)
            {
                proxy.Read();
                if (counter-- == 0)
                {
                    break;
                }
            }
            w.Stop();
            Console.WriteLine(w.Elapsed);
        }

        private class HashAttributs
        {
            public string Name;
            public ExtendedDatabaseInformationAttribute Attribute;

            public override bool Equals(object obj)
            {
                return GetHashCode() == obj?.GetHashCode();
            }

            public override int GetHashCode()
            {
                return (Name ?? string.Empty).GetHashCode();
            }
        }
        [TestMethod]
        public void AttributeTest()
        {

            var methodInfo = typeof(Repository.Calls.TestRepository).GetMethod(nameof(Repository.Calls.TestRepository.Test));

            var w = new Stopwatch();
            int counter = counts;
            w.Start();
            while (true)
            {
                var item = methodInfo.GetCustomAttribute<ExtendedDatabaseInformationAttribute>();
                string schema = item.Schema, database = item.Database;
                if (counter-- == 0)
                {
                    break;
                }
            }
            w.Stop();
            Console.WriteLine(w.Elapsed);
        }
        [TestMethod]
        public void AttributeTestHashed()
        {

            var methodInfo = typeof(Repository.Calls.TestRepository).GetMethod(nameof(Repository.Calls.TestRepository.Test));

            var w = new Stopwatch();
            int counter = counts;
            var hash = new HashSet<HashAttributs>();
            w.Start();
            while (true)
            {
                var ha = hash.FirstOrDefault(a => a.Name == methodInfo.Name) ?? new HashAttributs { Name = methodInfo.Name };
                if (ha.Attribute == null)
                {
                    var item = methodInfo.GetCustomAttribute<ExtendedDatabaseInformationAttribute>();
                    ha.Attribute = item;
                    hash.Add(ha);
                }
                string schema = ha.Attribute.Schema, database = ha.Attribute.Database;
                if (counter-- == 0)
                {
                    break;
                }
            }
            w.Stop();
            Console.WriteLine(w.Elapsed);
        }
        [TestMethod]
        public void AttributeTestDictionary()
        {

            var methodInfo = typeof(Repository.Calls.TestRepository).GetMethod(nameof(Repository.Calls.TestRepository.Test));

            var w = new Stopwatch();
            int counter = counts;
            var dict = new Dictionary<string, ExtendedDatabaseInformationAttribute>();
            w.Start();
            while (true)
            {
                ExtendedDatabaseInformationAttribute item = null;
                if (dict.ContainsKey(methodInfo.Name) == false)
                {
                    item = methodInfo.GetCustomAttribute<ExtendedDatabaseInformationAttribute>();
                    dict.Add(methodInfo.Name, item);
                }
                else
                {
                    item = dict[methodInfo.Name];
                }
                string schema = item.Schema, database = item.Database;
                if (counter-- == 0)
                {
                    break;
                }
            }
            w.Stop();
            Console.WriteLine(w.Elapsed);
        }

        [TestMethod]
        public void AttributeTest2()
        {

            var methodInfo = typeof(Repository.Calls.TestRepository).GetMethod(nameof(Repository.Calls.TestRepository.Test));

            var w = new Stopwatch();
            int counter = counts;
            w.Start();
            while (true)
            {
                if (counter-- == 0)
                {
                    break;
                }
            }
            w.Stop();
            Console.WriteLine(w.Elapsed);
        }
    }
}
