using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        public void SetProcess_SwitchesBetweenTwoProcesses()
        {
            // upstream issue #223: switching a record from one business process flow to another
            var processes = ContactProcessFlows().Take(2).ToList();

            if (processes.Count < 2)
            {
                Assert.Inconclusive("Switching needs two working business process flows on contact.");
            }

            var contact = CreateContact(null);

            // a new record gets an instance of the default process just after it's created; it would become the
            // active one if it arrived after the first switch
            for (var wait = 0; wait < 60 && ProcessInstances(contact).Count == 0; wait++)
            {
                Thread.Sleep(500);
            }

            // to the second process, then back to the first, whose old instance is reused, then to the second again
            foreach (var process in new[] { processes[1], processes[0], processes[1] })
            {
                // instances are ordered by when they last changed, to the second: switches in the same second tie
                Thread.Sleep(1500);
                Common.SetProcess(contact, process.ToEntityReference());

                // the active instance comes first
                var activeProcessId = ProcessInstances(contact).First().GetAttributeValue<EntityReference>(AttributeNames.ProcessId)?.Id;

                Assert.AreEqual(process.Id, activeProcessId, $"Active process after switching to {process.GetAttributeValue<string>(AttributeNames.Name)}");
            }

            foreach (var process in processes)
            {
                DeleteAfterTest(new EntityReference(Common.GetProcessEntityName(process.Id), Common.GetProcessInstance(contact, process.Id).Id));
            }
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
            var process = ContactProcessFlows().FirstOrDefault();

            if (process == null)
            {
                Assert.Inconclusive("This environment has no working business process flow on contact. Create one named \"WFT Test BPF\" with two stages.");
            }

            return process;
        }

        private DataCollection<Entity> ProcessInstances(EntityReference record)
        {
            return ((RetrieveProcessInstancesResponse)Service.Execute(new RetrieveProcessInstancesRequest
            {
                EntityId = record.Id,
                EntityLogicalName = record.LogicalName
            })).Processes.Entities;
        }

        /// <summary>
        /// The active business process flows on contact whose table exists, "WFT Test BPF" first.
        /// </summary>
        private List<Entity> ContactProcessFlows()
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
            return processes.Where(p => TableExists(p.GetAttributeValue<string>(AttributeNames.UniqueName))).ToList();
        }

        private EntityReference StartProcess(Entity process, EntityReference contact)
        {
            return Create(new Entity(process.GetAttributeValue<string>(AttributeNames.UniqueName))
            {
                ["bpf_contactid"] = contact,
                ["bpf_name"] = UniqueName("process")
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
