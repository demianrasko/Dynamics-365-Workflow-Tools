using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;
using System;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class DateFunctions_Tests
    {
        CrmService objService = new CrmService();

        [TestMethod]
        public void DateFunctions1()
        {
            var classObj = new Common(objService.service);

            var difference = new TimeSpan();
            var DayOfWeek = 0;
            var DayOfYear = 0;
            var Day = 0;
            var Month = 0;
            var Year = 0;
            var WeekOfYear = 0;
            classObj.DateFunctions(new DateTime(2017, 05, 05), new DateTime(2018, 01, 01), ref difference,
                ref DayOfWeek, ref DayOfYear, ref Day, ref Month, ref Year, ref WeekOfYear);

            Assert.AreEqual(difference.TotalMilliseconds, -20822400000);
            Assert.AreEqual(DayOfWeek,5);

            Assert.AreEqual(DayOfYear, 125);
            Assert.AreEqual(Day, 5);
            Assert.AreEqual(Month, 5);

            Assert.AreEqual(Year, 2017);

            Assert.AreEqual(WeekOfYear, 18);
        }

        [TestMethod]
        public void DateFunctions2()
        {
            var classObj = new Common(objService.service);

            var difference = new TimeSpan();
            var DayOfWeek = 0;
            var DayOfYear = 0;
            var Day = 0;
            var Month = 0;
            var Year = 0;
            var WeekOfYear = 0;
            classObj.DateFunctions(new DateTime(2019, 05, 05), new DateTime(2018, 01, 01), ref difference,
                ref DayOfWeek, ref DayOfYear, ref Day, ref Month, ref Year, ref WeekOfYear);

            Assert.AreEqual(difference.TotalMilliseconds, 42249600000);
            Assert.AreEqual(DayOfWeek, 0);
            Assert.AreEqual(DayOfYear, 125);
            Assert.AreEqual(Day, 5);
            Assert.AreEqual(Month, 5);
            Assert.AreEqual(Year, 2019);
            Assert.AreEqual(WeekOfYear, 18);
        }
    }
}

/*
    
*/
