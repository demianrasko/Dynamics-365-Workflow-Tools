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
            var classObj = new Common(objService.service);
            var rate=classObj.CurrencyConvert(1, "EUR", "USD");
            Assert.IsTrue(rate != 0);
        }
        [TestMethod]
        public void CurrencyConvert2()
        {
            var classObj = new Common(objService.service);
            var rate = classObj.CurrencyConvert(1, "USD", "EUR");
            Assert.IsTrue(rate != 0);
        }

        [TestMethod]
        public void CurrencyConvert3()
        {
            var classObj = new Common(objService.service);
            var rate = classObj.CurrencyConvert((decimal)100.35, "EUR", "GBP");
            Assert.IsTrue(rate != 0);
        }
        [TestMethod]
        public void CurrencyConvert4()
        {
            var classObj = new Common(objService.service);
            var rate = classObj.CurrencyConvert((decimal)11231300.30055, "JPY", "EUR");
            Assert.IsTrue(rate != 0);
        }

        [TestMethod]
        public void CurrencyConvert_SameCurrencyReturnsAmount()
        {
            var classObj = new Common(objService.service);
            Assert.AreEqual(12.34m, classObj.CurrencyConvert(12.34m, "usd", "USD"));
        }

        [TestMethod]
        public void ParseCurrencyConversion_ReadsRateAsDecimal()
        {
            var amount = Utility.ParseCurrencyConversion("{\"amount\":10.0,\"base\":\"USD\",\"date\":\"2026-10-02\",\"rates\":{\"EUR\":8.9087}}", "USD", "EUR");
            Assert.AreEqual(8.9087m, amount);
        }

        [TestMethod]
        [ExpectedException(typeof(Microsoft.Xrm.Sdk.InvalidPluginExecutionException))]
        public void ParseCurrencyConversion_UnsupportedCurrencyThrows()
        {
            Utility.ParseCurrencyConversion("{\"message\":\"not found\"}", "EUR", "ARS");
        }
    }
}
