using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;
using System;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class DateFunctions_Tests
    {
        [TestMethod]
        public void DateFunctions1()
        {
            var difference = new TimeSpan();
            var DayOfWeek = 0;
            var DayOfYear = 0;
            var Day = 0;
            var Month = 0;
            var Year = 0;
            var WeekOfYear = 0;
            Utility.DateFunctions(new DateTime(2017, 05, 05), new DateTime(2018, 01, 01), ref difference,
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
            // written for the es-ES culture (day/month dates, Monday-first ISO-style weeks)
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
            try
            {
                var difference = new TimeSpan();
                var DayOfWeek = 0;
                var DayOfYear = 0;
                var Day = 0;
                var Month = 0;
                var Year = 0;
                var WeekOfYear = 0;
                Utility.DateFunctions(new DateTime(2019, 05, 05), new DateTime(2018, 01, 01), ref difference,
                    ref DayOfWeek, ref DayOfYear, ref Day, ref Month, ref Year, ref WeekOfYear);

                Assert.AreEqual(difference.TotalMilliseconds, 42249600000);
                Assert.AreEqual(DayOfWeek, 0);
                Assert.AreEqual(DayOfYear, 125);
                Assert.AreEqual(Day, 5);
                Assert.AreEqual(Month, 5);
                Assert.AreEqual(Year, 2019);
                Assert.AreEqual(WeekOfYear, 18);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        private static int[] Run(DateTime date1, DateTime date2, string culture, out TimeSpan difference)
        {
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);

            try
            {
                difference = TimeSpan.Zero;
                int dayOfWeek = 0, dayOfYear = 0, day = 0, month = 0, year = 0, weekOfYear = 0;

                Utility.DateFunctions(date1, date2, ref difference, ref dayOfWeek, ref dayOfYear, ref day, ref month, ref year, ref weekOfYear);

                return new[] { dayOfWeek, dayOfYear, day, month, year, weekOfYear };
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [TestMethod]
        public void Difference_IsDate1MinusDate2()
        {
            Run(new DateTime(2024, 3, 1, 12, 0, 0), new DateTime(2024, 2, 28, 6, 0, 0), "en-US", out var difference);

            Assert.AreEqual(2.25, difference.TotalDays);
            Assert.AreEqual(54, difference.TotalHours);
        }

        [TestMethod]
        public void Parts_ComeFromDate1()
        {
            // 31 December 2024 is a Tuesday in a leap year
            CollectionAssert.AreEqual(new[] { 2, 366, 31, 12, 2024, 53 }, Run(new DateTime(2024, 12, 31), DateTime.MinValue, "en-US", out _));
        }

        [TestMethod]
        public void DayOfWeek_SundayIsZero()
        {
            Assert.AreEqual(0, Run(new DateTime(2026, 10, 4), DateTime.MinValue, "en-US", out _)[0]);
        }

        [TestMethod]
        public void WeekOfYear_FollowsTheCultureRules()
        {
            // 1 January 2027 is a Friday: week 1 in en-US (first day), week 53 of 2026 in de-DE (first four-day week)
            Assert.AreEqual(1, Run(new DateTime(2027, 1, 1), DateTime.MinValue, "en-US", out _)[5]);
            Assert.AreEqual(53, Run(new DateTime(2027, 1, 1), DateTime.MinValue, "de-DE", out _)[5]);
        }
    }
}
