using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;

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

        [TestMethod]
        public void Required_ReturnsTheValueOrNamesTheInput()
        {
            Assert.AreEqual("x", Utility.Required("x", "Name"));
            Assert.AreEqual("Name is required.", Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.Required(string.Empty, "Name")).Message);
            Assert.AreEqual("Name is required.", Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.Required((string)null, "Name")).Message);

            var lookup = new EntityReference("account", Guid.NewGuid());
            Assert.AreSame(lookup, Utility.Required(lookup, "Account"));
            Assert.AreEqual("Account is required.", Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.Required((EntityReference)null, "Account")).Message);
        }

        [TestMethod]
        public void RequiredGuid_ParsesTheTextOrNamesTheInput()
        {
            var id = Guid.NewGuid();

            Assert.AreEqual(id, Utility.RequiredGuid($" {{{id}}} ", "Record ID"));
            Assert.AreEqual("Record ID is required.", Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.RequiredGuid(null, "Record ID")).Message);
            Assert.AreEqual("Record ID 'abc' is not a valid GUID.", Assert.ThrowsException<InvalidPluginExecutionException>(() => Utility.RequiredGuid("abc", "Record ID")).Message);
        }
    }
}
