using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
        [TestMethod]
        public void HandleExceptions_NullReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, Utility.HandleExceptions(null));
        }

        [TestMethod]
        public void HandleExceptions_IncludesOrganizationServiceFaultDetails()
        {
            var fault = new OrganizationServiceFault
            {
                ErrorCode = -2147220891,
                Message = "Record is locked",
                TraceText = "plugin trace",
                InnerFault = new OrganizationServiceFault { ErrorCode = -2147220970, Message = "inner fault message" }
            };
            var ex = new System.ServiceModel.FaultException<OrganizationServiceFault>(fault, new System.ServiceModel.FaultReason("Record is locked"));

            var text = Utility.HandleExceptions(ex);

            StringAssert.Contains(text, "Code:\t-2147220891");
            StringAssert.Contains(text, "Fault Message:\tRecord is locked");
            StringAssert.Contains(text, "Trace:\tplugin trace");
            StringAssert.Contains(text, "--- Inner fault ---");
            StringAssert.Contains(text, "Code:\t-2147220970");
        }

        [TestMethod]
        public void HandleExceptions_WalksInnerExceptionsOfAnyType()
        {
            var ex = new InvalidPluginExecutionException("outer", new System.InvalidOperationException("middle", new System.ArgumentException("innermost")));

            var text = Utility.HandleExceptions(ex);

            StringAssert.Contains(text, "Message:\touter");
            StringAssert.Contains(text, "Message:\tmiddle");
            StringAssert.Contains(text, "Message:\tinnermost");
            StringAssert.Contains(text, "Type:\tSystem.ArgumentException");
        }
    }
}
