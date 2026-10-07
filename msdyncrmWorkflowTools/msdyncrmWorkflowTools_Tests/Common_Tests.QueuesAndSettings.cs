using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void SetUserSettings_UpdatesTheUsersSettings()
        {
            var userId = Guid.NewGuid();

            common.SetUserSettings(userId, 50, 0, 0, 0, 0, -1, true);

            var settings = service.Updated.Single();
            Assert.AreEqual(EntityNames.UserSettings, settings.LogicalName);
            Assert.AreEqual(userId, settings["systemuserid"]);
            Assert.AreEqual(50, settings["paginglimit"]);
        }

        [TestMethod]
        public void PickFromQueue_PicksEachItemForTheWorker()
        {
            var items = new[] { new Entity("queueitem", Guid.NewGuid()), new Entity("queueitem", Guid.NewGuid()) };
            service.OnRetrieveMultiple = query => Collection(items);
            service.OnExecute = r => new OrganizationResponse();

            Assert.AreEqual(2, common.PickFromQueue(Guid.NewGuid(), UserId, true, 2));

            var picks = service.Executed.Cast<PickFromQueueRequest>().ToList();
            CollectionAssert.AreEqual(items.Select(i => i.Id).ToArray(), picks.Select(r => r.QueueItemId).ToArray());
            Assert.IsTrue(picks.All(r => r.WorkerId == UserId && r.RemoveQueueItem));
            Assert.AreEqual(2, ((QueryExpression)service.Queries.Single()).TopCount);
        }

        [TestMethod]
        public void OrganizationSettings_AreReadAndWrittenTyped()
        {
            var organization = new Entity("organization", Guid.NewGuid()) { ["maxuploadfilesize"] = 5242880 };
            service.OnRetrieveMultiple = query => Collection(organization);

            Assert.AreEqual(5242880, common.GetOrganizationSetting("maxuploadfilesize"));
            Assert.IsTrue(common.SetOrganizationSetting("maxuploadfilesize", "10485760"));

            var update = service.Updated.Single();
            Assert.AreEqual(organization.Id, update.Id);
            Assert.AreEqual(10485760, update["maxuploadfilesize"]);
        }

        [TestMethod]
        public void QueueItems_UnassignedAndTop()
        {
            var query = Common.QueueItemsQuery(IdA, onlyUnassigned: true, top: 5);

            Assert.AreEqual(5, query.TopCount);
            AssertCondition(query.Criteria.Conditions[0], "statecode", ConditionOperator.Equal, 0);
            AssertCondition(query.Criteria.Conditions[1], "workerid", ConditionOperator.Null);
            AssertCondition(query.Criteria.Conditions[2], "queueid", ConditionOperator.Equal, IdA);

            var all = Common.QueueItemsQuery(IdA, onlyUnassigned: false);
            Assert.IsFalse(all.Criteria.Conditions.Any(c => c.AttributeName == "workerid"));
            Assert.IsNull(all.TopCount);
        }

        [TestMethod]
        public void CountQueueItems_CountsTheItemsOfTheQueue()
        {
            var queueId = Guid.NewGuid();
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.QueueItem, Guid.NewGuid()), new Entity(EntityNames.QueueItem, Guid.NewGuid()));

            Assert.AreEqual(2, common.CountQueueItems(queueId, true));
            Assert.AreEqual(EntityNames.QueueItem, ((QueryExpression)service.Queries.Single()).EntityName);
        }

        [TestMethod]
        public void GetEnvironmentVariable_PrefersTheCurrentValue()
        {
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.EnvironmentVariableDefinition, Guid.NewGuid())
            {
                [AttributeNames.DefaultValue] = "default",
                [Common.EnvironmentVariableValueAlias] = new AliasedValue(EntityNames.EnvironmentVariableValue, AttributeNames.Value, "current")
            });

            Assert.AreEqual("current", common.GetEnvironmentVariable(" new_ApiUrl "));

            var sent = (QueryExpression)service.Queries.Single();
            AssertCondition(sent.Criteria.Conditions.Single(), AttributeNames.SchemaName, ConditionOperator.Equal, "new_ApiUrl");
            Assert.AreEqual(JoinOperator.LeftOuter, sent.LinkEntities.Single().JoinOperator);
        }

        [TestMethod]
        public void GetEnvironmentVariable_FallsBackToTheDefaultOrNull()
        {
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.EnvironmentVariableDefinition, Guid.NewGuid()) { [AttributeNames.DefaultValue] = "default" });
            Assert.AreEqual("default", common.GetEnvironmentVariable("new_ApiUrl"));

            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.EnvironmentVariableDefinition, Guid.NewGuid()));
            Assert.IsNull(common.GetEnvironmentVariable("new_ApiUrl"));

            service.OnRetrieveMultiple = query => Collection();
            Assert.IsNull(common.GetEnvironmentVariable("new_Missing"));
        }
    }
}
