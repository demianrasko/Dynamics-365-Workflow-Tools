using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
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
            var process = new EntityReference("workflow", Guid.NewGuid());
            var listId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            service.OnExecute = r => r is SendEmailRequest ? new SendEmailResponse { Results = { ["Subject"] = "Hello" } } : new OrganizationResponse();

            common.ApplyRoutingRule(record);
            common.SetProcess(record, process);
            common.CalculateRollupField(record, "new_total");
            common.AddListToCampaign(listId, campaignId);
            common.CopyListMembers(listId, campaignId);
            common.CopyDynamicListToStatic(listId);
            common.ResolveCase(RecordId, "Fixed", "Details");
            Assert.AreEqual("Hello", common.SendEmail(RecordId));

            var e = service.Executed;
            Assert.AreEqual(record, ((ApplyRoutingRuleRequest)e[0]).Target);
            Assert.AreEqual(process, ((SetProcessRequest)e[1]).NewProcess);
            Assert.AreEqual("new_total", ((CalculateRollupFieldRequest)e[2]).FieldName);
            Assert.AreEqual(campaignId, ((AddItemCampaignRequest)e[3]).CampaignId);
            Assert.AreEqual("list", ((AddItemCampaignRequest)e[3]).EntityName);
            Assert.AreEqual(listId, ((CopyMembersListRequest)e[4]).SourceListId);
            Assert.AreEqual(listId, ((CopyDynamicListToStaticRequest)e[5]).ListId);
            var close = (CloseIncidentRequest)e[6];
            Assert.AreEqual(5, close.Status.Value);
            Assert.AreEqual(RecordId, close.IncidentResolution.GetAttributeValue<EntityReference>("incidentid").Id);
            Assert.IsTrue(((SendEmailRequest)e[7]).IssueSend);
        }
    }
}
