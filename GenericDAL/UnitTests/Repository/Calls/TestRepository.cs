﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericDataAccessLayer.LazyDal;
using GenericDataAccessLayer.LazyDal.Attributes;

namespace UnitTests.Repository.Calls
{
    public interface TestRepository : IRepository
    {
        void CreateUseRef(ref User data);
        User CreateUseReturn(User data);
        User[] UpdateUseArrayReturn(IEnumerable<User> data);
        IEnumerable<User> Read();
        void Get(out User data);
        void Update(ref User data);
        void IncorrectCallFirstTrial(IEnumerable<string> data);
        void IncorrectCallSecondTrial(IEnumerable<DateTime> data);
        void OnExecutionException();
        List<User> MultiItems(IEnumerable value1, IEnumerable value2);
        string GetSomeText();
        int GetSomeInt();
        void DoSomething(IEnumerable data);
        void DoSomethingEGain(ref string item1, out DateTime item2);
        [ExtendedDatabaseInformation("test2", "mark2", CustomProcedureName = "Super")]
        void Test();
        [ExtendedDatabaseInformation(null, CustomProcedureName = "Test")]
        void Test2();
        [ExtendedDatabaseInformation("dbo2")]
        void Test3();
        [ExtendedDatabaseInformation(null)]
        void Test4();
    }
}
