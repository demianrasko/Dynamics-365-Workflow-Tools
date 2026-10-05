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
    }
}
