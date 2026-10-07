using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class CurrencyConvert_Tests
    {
        [TestMethod]
        public void CurrencyConvert1()
        {
            var rate=Utility.CurrencyConvert(1, "EUR", "USD");
            Assert.IsTrue(rate != 0);
        }
        [TestMethod]
        public void CurrencyConvert2()
        {
            var rate = Utility.CurrencyConvert(1, "USD", "EUR");
            Assert.IsTrue(rate != 0);
        }

        [TestMethod]
        public void CurrencyConvert3()
        {
            var rate = Utility.CurrencyConvert((decimal)100.35, "EUR", "GBP");
            Assert.IsTrue(rate != 0);
        }
        [TestMethod]
        public void CurrencyConvert4()
        {
            var rate = Utility.CurrencyConvert((decimal)11231300.30055, "JPY", "EUR");
            Assert.IsTrue(rate != 0);
        }

        [TestMethod]
        public void CurrencyConvert_SameCurrencyReturnsAmount()
        {
            Assert.AreEqual(12.34m, Utility.CurrencyConvert(12.34m, "usd", "USD"));
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
