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
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetProcessStageId_UnknownStageThrows()
        {
            common.GetProcessStageId(Guid.NewGuid(), "Close");
        }

        [TestMethod]
        public void GetProcessInstance_PicksTheInstanceOfTheProcess()
        {
            var processId = Guid.NewGuid();
            var other = Instance(Guid.NewGuid());
            var wanted = Instance(processId);
            service.OnExecute = r => InstancesResponse(other, wanted);

            Assert.AreSame(wanted, common.GetProcessInstance(new EntityReference("opportunity", RecordId), processId));
        }

        [TestMethod]
        public void GetProcessInstance_UsesTheFirstInstanceWhenInstancesHaveNoProcessId()
        {
            var first = new Entity("opportunitysalesprocess", Guid.NewGuid());
            service.OnExecute = r => InstancesResponse(first, new Entity("opportunitysalesprocess", Guid.NewGuid()));

            Assert.AreSame(first, common.GetProcessInstance(new EntityReference("opportunity", RecordId), Guid.NewGuid()));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetProcessInstance_NoInstanceOfTheProcessThrows()
        {
            service.OnExecute = r => InstancesResponse(Instance(Guid.NewGuid()));

            common.GetProcessInstance(new EntityReference("opportunity", RecordId), Guid.NewGuid());
        }

        [TestMethod]
        public void SetProcessStage_MovesTheInstanceToTheStage()
        {
            var processId = Guid.NewGuid();
            var stageId = Guid.NewGuid();
            var instance = Instance(processId);
            service.OnRetrieveMultiple = query => Collection(new Entity("processstage", stageId));
            service.OnExecute = r => InstancesResponse(instance);
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["uniquename"] = "new_bpf" };

            common.SetProcessStage(new EntityReference("opportunity", RecordId), new EntityReference("workflow", processId), "Close");

            var update = service.Updated.Single();
            Assert.AreEqual("new_bpf", update.LogicalName);
            Assert.AreEqual(instance.Id, update.Id);
            Assert.AreEqual(stageId, update.GetAttributeValue<EntityReference>("activestageid").Id);
        }

        [TestMethod]
        public void ConcatenateFromQuery_JoinsFormattedValuesUpToTheTop()
        {
            service.OnRetrieveMultiple = query => Collection(
                new Entity("account") { ["revenue"] = new Money(1234.5m) },
                new Entity("account"),
                new Entity("account") { ["revenue"] = new Money(10m) },
                new Entity("account") { ["revenue"] = new Money(99m) });

            Assert.AreEqual("1,234.50; 10.00", common.ConcatenateFromQuery("<fetch><entity name='account' /></fetch>", "revenue", "; ", "N2", 2));
        }

        [TestMethod]
        public void ConcatenateFromQuery_NoValuesIsNull()
        {
            Assert.IsNull(common.ConcatenateFromQuery("<fetch><entity name='account' /></fetch>", "name", ",", string.Empty, 0));
        }

        [TestMethod]
        public void SingleRequests_SendTheRightMessage()
        {
            var record = new EntityReference("incident", RecordId);
            var listId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            service.OnExecute = r => r is SendEmailRequest ? new SendEmailResponse { Results = { ["Subject"] = "Hello" } } : new OrganizationResponse();

            common.ApplyRoutingRule(record);
            common.CalculateRollupField(record, "new_total");
            common.AddListToCampaign(listId, campaignId);
            common.CopyListMembers(listId, campaignId);
            common.CopyDynamicListToStatic(listId);
            common.ResolveCase(RecordId, "Fixed", "Details");
            Assert.AreEqual("Hello", common.SendEmail(RecordId));

            var e = service.Executed;
            Assert.AreEqual(record, ((ApplyRoutingRuleRequest)e[0]).Target);
            Assert.AreEqual("new_total", ((CalculateRollupFieldRequest)e[1]).FieldName);
            Assert.AreEqual(campaignId, ((AddItemCampaignRequest)e[2]).CampaignId);
            Assert.AreEqual("list", ((AddItemCampaignRequest)e[2]).EntityName);
            Assert.AreEqual(listId, ((CopyMembersListRequest)e[3]).SourceListId);
            Assert.AreEqual(listId, ((CopyDynamicListToStaticRequest)e[4]).ListId);
            var close = (CloseIncidentRequest)e[5];
            Assert.AreEqual(5, close.Status.Value);
            Assert.AreEqual(RecordId, close.IncidentResolution.GetAttributeValue<EntityReference>("incidentid").Id);
            Assert.IsTrue(((SendEmailRequest)e[6]).IssueSend);
        }

        [TestMethod]
        public void SetProcess_LeavesANewInstanceAsItIs()
        {
            var process = new EntityReference("workflow", Guid.NewGuid());
            service.OnExecute = r => r is RetrieveProcessInstancesRequest ? InstancesResponse(Instance(process.Id), Instance(Guid.NewGuid())) : new OrganizationResponse();

            common.SetProcess(new EntityReference("contact", RecordId), process);

            Assert.AreEqual(process, ((SetProcessRequest)service.Executed[0]).NewProcess);
            Assert.AreEqual(0, service.Updated.Count);
        }

        [TestMethod]
        public void SetProcess_MakesAReusedInstanceTheActiveOne()
        {
            // upstream issue #223: switching back reuses the old instance, but the latest changed one is active
            var process = new EntityReference("workflow", Guid.NewGuid());
            var stage = new EntityReference("processstage", Guid.NewGuid());
            var reused = Instance(process.Id);
            service.OnExecute = r => r is RetrieveProcessInstancesRequest ? InstancesResponse(Instance(Guid.NewGuid()), reused) : new OrganizationResponse();
            service.OnRetrieve = (name, id, columns) => name == "workflow"
                ? new Entity(name, id) { ["uniquename"] = "new_bpf" }
                : new Entity(name, id) { ["activestageid"] = stage };

            common.SetProcess(new EntityReference("contact", RecordId), process);

            var update = service.Updated.Single();
            Assert.AreEqual("new_bpf", update.LogicalName);
            Assert.AreEqual(reused.Id, update.Id);
            Assert.AreEqual(stage, update["activestageid"]);
        }

        [TestMethod]
        public void GetProcessStageId_TrimsTheStageName()
        {
            var stageId = Guid.NewGuid();
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.ProcessStage, stageId));

            Assert.AreEqual(stageId, common.GetProcessStageId(Guid.NewGuid(), " Develop "));
            Assert.IsTrue(((QueryExpression)service.Queries.Single()).Criteria.Conditions.Any(c => c.Values.Contains("Develop")));
        }

        [TestMethod]
        public void ConcatenateFromQuery_ExpandsANewLineSeparator()
        {
            service.OnRetrieveMultiple = query => Collection(new Entity("account") { ["name"] = "A" }, new Entity("account") { ["name"] = "B" });

            Assert.AreEqual("A\nB", common.ConcatenateFromQuery("<fetch><entity name='account' /></fetch>", "name", @"\n", string.Empty, 0));
        }
    }
}
