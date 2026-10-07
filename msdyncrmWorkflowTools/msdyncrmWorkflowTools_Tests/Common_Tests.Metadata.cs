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
        public void GetOptionSetValue_ReturnsTheValueOrZero()
        {
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["industrycode"] = new OptionSetValue(7) };

            Assert.AreEqual(7, common.GetOptionSetValue(new EntityReference("account", RecordId), "industrycode"));
            Assert.AreEqual("industrycode", service.Retrieved.Single().Columns.Single());
            Assert.AreEqual(0, common.GetOptionSetValue(new EntityReference("account", RecordId), "missingcode"));
        }

        [TestMethod]
        public void InsertOptionValue_GlobalOptionSetUsesTheOptionSetName()
        {
            service.OnExecute = r => new InsertOptionValueResponse();

            common.InsertOptionValue(true, "new_size", "account", "Large", 3, 1033);

            var request = (InsertOptionValueRequest)service.Executed.Single();
            Assert.AreEqual("new_size", request.OptionSetName);
            Assert.IsNull(request.EntityLogicalName);
            Assert.AreEqual(3, request.Value);
            Assert.AreEqual("Large", request.Label.LocalizedLabels[0].Label);
        }

        [TestMethod]
        public void InsertOptionValue_LocalOptionSetUsesTheAttributeAndEntity()
        {
            service.OnExecute = r => new InsertOptionValueResponse();

            common.InsertOptionValue(false, "new_size", "account", "Large", 3, 1033);

            var request = (InsertOptionValueRequest)service.Executed.Single();
            Assert.IsNull(request.OptionSetName);
            Assert.AreEqual("new_size", request.AttributeLogicalName);
            Assert.AreEqual("account", request.EntityLogicalName);
        }

        [TestMethod]
        public void DeleteOptionValue_LocalOptionSetUsesTheAttributeAndEntity()
        {
            service.OnExecute = r => new OrganizationResponse();

            common.DeleteOptionValue(false, "new_size", "account", 3);

            var request = (DeleteOptionValueRequest)service.Executed.Single();
            Assert.AreEqual("new_size", request.AttributeLogicalName);
            Assert.AreEqual("account", request.EntityLogicalName);
            Assert.AreEqual(3, request.Value);
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
    }
}
