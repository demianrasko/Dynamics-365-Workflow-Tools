using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public partial class Common_Tests
    {
        private static readonly Guid RecordId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid TeamId = Guid.NewGuid();

        private FakeOrganizationService service;
        private TraceRecorder trace;
        private Common common;

        [TestInitialize]
        public void Setup()
        {
            service = new FakeOrganizationService();
            trace = new TraceRecorder();
            common = new Common(service, trace);
        }

        private static string UrlFor(string entityName)
        {
            return $"https://org.crm.dynamics.com/main.aspx?etn={entityName}&id={RecordId}";
        }

        private static RetrieveMetadataChangesResponse MetadataResponse(string schemaName)
        {
            return new RetrieveMetadataChangesResponse
            {
                Results = { ["EntityMetadata"] = new EntityMetadataCollection { new EntityMetadata { SchemaName = schemaName } } }
            };
        }

        private static EntityMetadata MetadataWithTypeCode(int objectTypeCode)
        {
            // ObjectTypeCode has no public setter
            var metadata = new EntityMetadata();
            typeof(EntityMetadata).GetProperty("ObjectTypeCode").SetValue(metadata, (int?)objectTypeCode);

            return metadata;
        }

        private static EntityMetadata EntityWithAttributes(string primaryIdAttribute, params AttributeMetadata[] attributes)
        {
            // the metadata classes have no public setters for these
            var metadata = new EntityMetadata();
            typeof(EntityMetadata).GetProperty("PrimaryIdAttribute").SetValue(metadata, primaryIdAttribute);
            typeof(EntityMetadata).GetProperty("Attributes").SetValue(metadata, attributes);

            return metadata;
        }

        private static T Attribute<T>(string logicalName) where T : AttributeMetadata, new()
        {
            var attribute = new T { LogicalName = logicalName };
            typeof(AttributeMetadata).GetProperty("IsValidForCreate").SetValue(attribute, (bool?)true);
            typeof(AttributeMetadata).GetProperty("IsValidForUpdate").SetValue(attribute, (bool?)true);
            typeof(AttributeMetadata).GetProperty("IsPrimaryId").SetValue(attribute, (bool?)false);
            typeof(AttributeMetadata).GetProperty("IsPrimaryName").SetValue(attribute, (bool?)false);
            typeof(AttributeMetadata).GetProperty("AttributeTypeName").SetValue(attribute, AttributeTypeDisplayName.StringType);

            return attribute;
        }

        private static RetrieveAttributeResponse AttributeResponse(bool isSecured)
        {
            return new RetrieveAttributeResponse
            {
                Results = { ["AttributeMetadata"] = new MoneyAttributeMetadata { IsSecured = isSecured, MetadataId = Guid.NewGuid() } }
            };
        }

        private static RetrieveProcessInstancesResponse InstancesResponse(params Entity[] instances)
        {
            return new RetrieveProcessInstancesResponse { Results = { ["Processes"] = Collection(instances) } };
        }

        private static Entity Instance(Guid processId)
        {
            return new Entity("opportunitysalesprocess", Guid.NewGuid())
            {
                ["processid"] = new EntityReference("workflow", processId),
                ["name"] = "Sales process"
            };
        }

        private static EntityCollection Collection(params Entity[] entities)
        {
            return new EntityCollection(entities.ToList());
        }

        private static EntityCollection Page(bool moreRecords, string cookie, params Entity[] entities)
        {
            var page = Collection(entities);
            page.MoreRecords = moreRecords;
            page.PagingCookie = cookie;

            return page;
        }

        private static OptionSetValueCollection Options(params int[] values)
        {
            return new OptionSetValueCollection(values.Select(v => new OptionSetValue(v)).ToList());
        }

        private static int[] Values(Entity entity, string attributeName)
        {
            return entity.GetAttributeValue<OptionSetValueCollection>(attributeName).Select(v => v.Value).ToArray();
        }

        private static readonly Guid IdA = Guid.NewGuid();
        private static readonly Guid IdB = Guid.NewGuid();

        private static void AssertCondition(ConditionExpression condition, string attribute, ConditionOperator op, params object[] values)
        {
            Assert.AreEqual(attribute, condition.AttributeName);
            Assert.AreEqual(op, condition.Operator);
            CollectionAssert.AreEqual(values, condition.Values.ToArray());
        }
    }
}
