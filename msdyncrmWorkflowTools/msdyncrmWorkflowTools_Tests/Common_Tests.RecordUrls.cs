using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void ParseRecordUrl_UsesEtnWithoutAMetadataCall()
        {
            var parsedUrl = common.ParseRecordUrl($"https://org.crm.dynamics.com/main.aspx?etn=account&id={RecordId}&pagetype=entityrecord");

            Assert.AreEqual("account", parsedUrl.EntityName);
            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        public void ParseRecordUrl_LooksUpTheEntityNameFromTheTypeCode()
        {
            service.OnExecute = r => MetadataResponse("Account");

            var parsedUrl = common.ParseRecordUrl($"https://org.crm.dynamics.com/main.aspx?etc=1&id={RecordId}");

            Assert.AreEqual("account", parsedUrl.EntityName);
            Assert.AreEqual(RecordId, parsedUrl.Id);
            Assert.IsInstanceOfType(service.Executed.Single(), typeof(RetrieveMetadataChangesRequest));
        }

        [TestMethod]
        public void GetEntityTypeCode_ReadsTheObjectTypeCode()
        {
            service.OnExecute = r => new RetrieveMetadataChangesResponse
            {
                Results = { ["EntityMetadata"] = new EntityMetadataCollection { MetadataWithTypeCode(2) } }
            };

            Assert.AreEqual(2, common.GetEntityTypeCode("contact"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetEntityTypeCode_UnknownEntityThrows()
        {
            service.OnExecute = r => new RetrieveMetadataChangesResponse { Results = { ["EntityMetadata"] = new EntityMetadataCollection() } };

            common.GetEntityTypeCode("new_missing");
        }

        [TestMethod]
        public void GetRecordReference_BuildsTheReference()
        {
            service.OnExecute = r => MetadataResponse("Contact");

            var reference = common.GetRecordReference($"https://org.crm.dynamics.com/main.aspx?etc=2&id={RecordId}");

            Assert.AreEqual("contact", reference.LogicalName);
            Assert.AreEqual(RecordId, reference.Id);
        }
    }
}
