using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
        [TestMethod]
        public void NumericOperation_SymbolsAndNames()
        {
            Assert.AreEqual(7m, Utility.NumericOperation(5m, "+", 2m));
            Assert.AreEqual(3m, Utility.NumericOperation(5m, " subtract ", 2m));
            Assert.AreEqual(10m, Utility.NumericOperation(5m, "X", 2m));
            Assert.AreEqual(2.5m, Utility.NumericOperation(5m, "/", 2m));
            Assert.AreEqual(1m, Utility.NumericOperation(5m, "mod", 2m));
            Assert.AreEqual(2m, Utility.NumericOperation(5m, "Min", 2m));
            Assert.AreEqual(5m, Utility.NumericOperation(5m, "max", 2m));
        }

        [TestMethod]
        public void NumericOperation_ExplainsAnUnknownOperation()
        {
            var ex = Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.NumericOperation(5m, "^", 2m));

            StringAssert.Contains(ex.Message, "'^'");
            Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.NumericOperation(5m, null, 2m));
        }

        [TestMethod]
        public void NumericOperation_ExplainsADivisionByZero()
        {
            Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.NumericOperation(5m, "/", 0m));
            Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.NumericOperation(5m, "%", 0m));
        }

        [TestMethod]
        public void DivideOrZero_IsZeroForADivisionByZero()
        {
            Assert.AreEqual(2.5m, Utility.DivideOrZero(5m, 2m));
            Assert.AreEqual(0m, Utility.DivideOrZero(5m, 0m));
        }
    }
}
