using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class CurrencyConvert_Tests
    {
        CrmService objService = new CrmService();
        [TestMethod]
        public void CurrencyConvert1()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            decimal rate=classObj.CurrencyConvert(1, "EUR", "USD");
            Assert.IsTrue(rate != 0);
        }
        [TestMethod]
        public void CurrencyConvert2()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            decimal rate = classObj.CurrencyConvert(1, "USD", "EUR");
            Assert.IsTrue(rate != 0);
        }

        [TestMethod]
        public void CurrencyConvert3()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            decimal rate = classObj.CurrencyConvert((decimal)100.35, "EUR", "ARS");
            Assert.IsTrue(rate != 0);
        }
        [TestMethod]
        public void CurrencyConvert4()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            decimal rate = classObj.CurrencyConvert((decimal)11231300.30055, "CLP", "EUR");
            Assert.IsTrue(rate != 0);
        }
    }
}
