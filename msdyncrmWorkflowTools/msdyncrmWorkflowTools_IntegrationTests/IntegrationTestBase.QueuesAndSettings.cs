using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void GetEnvironmentVariable_UsesTheCurrentValueOverTheDefault()
        {
            var schemaName = $"new_WftTest{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var definition = Create(new Entity(EntityNames.EnvironmentVariableDefinition)
            {
                [AttributeNames.SchemaName] = schemaName,
                ["displayname"] = UniqueName("variable"),
                ["type"] = new OptionSetValue(100000000), // String
                [AttributeNames.DefaultValue] = "default value"
            });

            Assert.AreEqual("default value", Common.GetEnvironmentVariable(schemaName));

            Create(new Entity(EntityNames.EnvironmentVariableValue)
            {
                [AttributeNames.SchemaName] = schemaName,
                [AttributeNames.EnvironmentVariableDefinitionId] = definition,
                [AttributeNames.Value] = "current value"
            });

            Assert.AreEqual("current value", Common.GetEnvironmentVariable(schemaName));
            Assert.IsNull(Common.GetEnvironmentVariable($"new_WftMissing{Guid.NewGuid():N}"));
        }

        [TestMethod]
        public void Queues_CountAndPickItems()
        {
            var queue = Create(new Entity(EntityNames.Queue) { [AttributeNames.Name] = UniqueName("queue") });
            var task = Create(new Entity("task") { [AttributeNames.Subject] = UniqueName("task") });
            Service.Execute(new AddToQueueRequest { DestinationQueueId = queue.Id, Target = task });

            Assert.AreEqual(1, Common.CountQueueItems(queue.Id, true));
            Assert.AreEqual(1, Common.PickFromQueue(queue.Id, UserId, false, 1));
            Assert.AreEqual(0, Common.CountQueueItems(queue.Id, true));
            Assert.AreEqual(1, Common.CountQueueItems(queue.Id, false));
        }

        [TestMethod]
        public void OrganizationSettings_ReadAndWrite()
        {
            var original = (string)Common.GetOrganizationSetting("trackingprefix");

            try
            {
                Assert.IsTrue(Common.SetOrganizationSetting("trackingprefix", "WFT:"));
                Assert.AreEqual("WFT:", Common.GetOrganizationSetting("trackingprefix"));
            }
            finally
            {
                Common.SetOrganizationSetting("trackingprefix", original ?? string.Empty);
            }

            Assert.IsInstanceOfType(Common.GetOrganizationSetting("maxuploadfilesize"), typeof(int));
        }

        [TestMethod]
        public void SetUserSettings_ChangesOnlyTheSuppliedSettings()
        {
            var columns = new ColumnSet("paginglimit", "timezonecode", "issendasallowed");
            var settings = Service.RetrieveMultiple(new QueryExpression(EntityNames.UserSettings)
            {
                ColumnSet = columns,
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.SystemUserId, ConditionOperator.Equal, UserId) } }
            }).Entities.Single();
            var paging = settings.GetAttributeValue<int>("paginglimit");
            var newPaging = paging == 50 ? 100 : 50;

            try
            {
                Common.SetUserSettings(UserId, newPaging, 0, 0, 0, 0, -1, null);

                var updated = Service.Retrieve(EntityNames.UserSettings, settings.Id, columns);
                Assert.AreEqual(newPaging, updated.GetAttributeValue<int>("paginglimit"));
                Assert.AreEqual(settings.GetAttributeValue<int>("timezonecode"), updated.GetAttributeValue<int>("timezonecode"));
                Assert.AreEqual(settings.GetAttributeValue<bool>("issendasallowed"), updated.GetAttributeValue<bool>("issendasallowed"));
            }
            finally
            {
                Common.SetUserSettings(UserId, paging, 0, 0, 0, 0, -1, null);
            }
        }
    }
}
