using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System;
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
        public void GetRecordReference_BuildsTheReference()
        {
            service.OnExecute = r => MetadataResponse("Contact");

            var reference = common.GetRecordReference($"https://org.crm.dynamics.com/main.aspx?etc=2&id={RecordId}");

            Assert.AreEqual("contact", reference.LogicalName);
            Assert.AreEqual(RecordId, reference.Id);
        }

        [TestMethod]
        public void GetAppModuleId_UnknownAppThrowsAClearError()
        {
            var error = Assert.ThrowsException<InvalidPluginExecutionException>(() => common.GetAppModuleId("nope"));

            StringAssert.Contains(error.Message, "'nope'");
        }

        [TestMethod]
        public void GetRecordReference_RequiredUrlNamesTheInput()
        {
            Assert.AreEqual(RecordId, common.GetRecordReference(UrlFor(EntityNames.Account), "Record URL").Id);
            AssertRequired("Record URL", () => common.GetRecordReference(string.Empty, "Record URL"));
        }

        [TestMethod]
        public void GetRecordUrl_BuildsTheUrlFromTheReference()
        {
            var id = Guid.NewGuid();
            service.OnExecute = r => new RetrieveMetadataChangesResponse
            {
                Results = { ["EntityMetadata"] = new EntityMetadataCollection { MetadataWithTypeCode(2) } }
            };

            var url = common.GetRecordUrl(UrlFor(EntityNames.Account), $" {id} ", EntityNames.Contact);

            Assert.AreEqual($"https://org.crm.dynamics.com/main.aspx?etc=2&id={id}&etn=contact&pagetype=entityrecord", url);
        }

        [TestMethod]
        public void GetRecordUrl_ExplainsMissingOrInvalidInputs()
        {
            Assert.ThrowsException<InvalidPluginExecutionException>(() => common.GetRecordUrl(string.Empty, Guid.NewGuid().ToString(), EntityNames.Contact));
            Assert.ThrowsException<InvalidPluginExecutionException>(() => common.GetRecordUrl(UrlFor(EntityNames.Account), Guid.NewGuid().ToString(), null));
            Assert.AreEqual("Record ID 'x' is not a valid GUID.",
                Assert.ThrowsException<InvalidPluginExecutionException>(() => common.GetRecordUrl(UrlFor(EntityNames.Account), "x", EntityNames.Contact)).Message);
            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        public void GetMobileDeepLinks_UseTheRecordsTableAndId()
        {
            var links = common.GetMobileDeepLinks(UrlFor(EntityNames.Contact));

            Assert.AreEqual($"ms-dynamicsxrm://?pagetype=entity&etn=contact&id={RecordId}", links.Edit);
            Assert.AreEqual("ms-dynamicsxrm://?pagetype=create&etn=contact", links.New);
            Assert.AreEqual("ms-dynamicsxrm://?pagetype=view&etn=contact", links.DefaultView);
            AssertRequired("Record URL", () => common.GetMobileDeepLinks(null));
        }
    }
}
