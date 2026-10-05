using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Process tests. They use a working business process flow on contact ("WFT Test BPF" when there is one), and
    /// never start an existing workflow (those may send email or change data); ExecuteWorkflow only runs a workflow
    /// named "WFT Test" that was made for these tests.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void ConcatenateFromQuery_JoinsTheChildrensValues()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("parent") });
            CreateContact(account);
            CreateContact(account);
            var fetchXml = $@"<fetch><entity name='contact'><attribute name='lastname' /><order attribute='lastname' />
                <filter><condition attribute='parentcustomerid' operator='eq' value='{account.Id}' /></filter></entity></fetch>";

            var joined = Common.ConcatenateFromQuery(fetchXml, "lastname", @" | ", string.Empty, 0);

            Assert.AreEqual(2, joined.Split('|').Length);
            Assert.IsNull(Common.ConcatenateFromQuery(fetchXml.Replace(account.Id.ToString(), System.Guid.NewGuid().ToString()), "lastname", ", ", string.Empty, 0));
        }

        [TestMethod]
        public void BusinessProcessFlow_FindsTheInstanceAndMovesItsStage()
        {
            var process = ContactProcessFlow();
            var contact = CreateContact(null);
            var instance = StartProcess(process, contact);
            var entityName = Common.GetProcessEntityName(process.Id);

            Assert.AreEqual(process.GetAttributeValue<string>(AttributeNames.UniqueName), entityName);
            Assert.AreEqual(instance.Id, Common.GetProcessInstance(contact, process.Id).Id);

            var current = Read(instance, AttributeNames.ActiveStageId).GetAttributeValue<EntityReference>(AttributeNames.ActiveStageId)?.Id;
            var other = Stages(process).First(s => s.Id != current);
            var stageName = other.GetAttributeValue<string>(AttributeNames.StageName);

            Assert.AreEqual(other.Id, Common.GetProcessStageId(process.Id, $" {stageName} "));

            Common.SetProcessStage(contact, process.ToEntityReference(), stageName);

            Assert.AreEqual(other.Id, Read(instance, AttributeNames.ActiveStageId).GetAttributeValue<EntityReference>(AttributeNames.ActiveStageId).Id);
        }

        [TestMethod]
        public void SetProcess_SwitchesTheRecordsProcess()
        {
            var process = ContactProcessFlow();
            var contact = CreateContact(null);

            Common.SetProcess(contact, process.ToEntityReference());

            var instance = Common.GetProcessInstance(contact, process.Id);
            DeleteAfterTest(new EntityReference(Common.GetProcessEntityName(process.Id), instance.Id));
        }

        [TestMethod]
        public void ExecuteWorkflow_RunsTheTestWorkflow()
        {
            var workflow = Service.RetrieveMultiple(new QueryExpression(EntityNames.Workflow)
            {
                ColumnSet = new ColumnSet("primaryentity"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(AttributeNames.Name, ConditionOperator.Equal, "WFT Test"),
                        new ConditionExpression("type", ConditionOperator.Equal, 1),
                        new ConditionExpression(AttributeNames.StateCode, ConditionOperator.Equal, 1)
                    }
                }
            }).Entities.FirstOrDefault();

            if (workflow == null || workflow.GetAttributeValue<string>("primaryentity") != EntityNames.Account)
            {
                Assert.Inconclusive("Create an activated on-demand workflow on account named \"WFT Test\" to test ExecuteWorkflow.");
            }

            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("workflow") });

            Common.ExecuteWorkflow(workflow.Id, new[] { account.Id });
        }

        /// <summary>
        /// An active business process flow on contact whose table exists ("WFT Test BPF" first), or inconclusive
        /// when there is none.
        /// </summary>
        private Entity ContactProcessFlow()
        {
            var processes = Service.RetrieveMultiple(new QueryExpression(EntityNames.Workflow)
            {
                ColumnSet = new ColumnSet(AttributeNames.UniqueName, AttributeNames.Name),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("category", ConditionOperator.Equal, 4),
                        new ConditionExpression("primaryentity", ConditionOperator.Equal, EntityNames.Contact),
                        new ConditionExpression("type", ConditionOperator.Equal, 1),
                        new ConditionExpression(AttributeNames.StateCode, ConditionOperator.Equal, 1)
                    }
                }
            }).Entities.OrderBy(p => p.GetAttributeValue<string>(AttributeNames.Name) == "WFT Test BPF" ? 0 : 1);

            // a process whose instance table is missing is broken (Dataverse itself can't use it)
            var process = processes.FirstOrDefault(p => TableExists(p.GetAttributeValue<string>(AttributeNames.UniqueName)));

            if (process == null)
            {
                Assert.Inconclusive("This environment has no working business process flow on contact. Create one named \"WFT Test BPF\" with two stages.");
            }

            return process;
        }

        private EntityReference StartProcess(Entity process, EntityReference contact)
        {
            return Create(new Entity(process.GetAttributeValue<string>(AttributeNames.UniqueName))
            {
                ["bpf_contactid"] = contact,
                [AttributeNames.Name] = UniqueName("process")
            });
        }

        private Entity[] Stages(Entity process)
        {
            return Service.RetrieveMultiple(new QueryExpression(EntityNames.ProcessStage)
            {
                ColumnSet = new ColumnSet(AttributeNames.StageName),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.ProcessId, ConditionOperator.Equal, process.Id) } }
            }).Entities.ToArray();
        }
    }
}
