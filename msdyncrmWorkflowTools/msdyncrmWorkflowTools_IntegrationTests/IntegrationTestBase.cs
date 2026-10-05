using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Base for the integration tests: connects <see cref="Common"/> to a real environment and deletes every record a
    /// test creates through <see cref="Create"/> when the test ends, even when it fails. The tests in this class run
    /// against both test environments (see the subclasses); tests that need Dynamics 365 tables live in
    /// <see cref="Dynamics365_IntegrationTests"/>. Without the environment variable the tests are inconclusive.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        /// <summary>Prefix of every record the tests create, so leftovers are easy to find.</summary>
        protected const string Prefix = "WFT-Test";

        private readonly List<EntityReference> created = new List<EntityReference>();

        protected IOrganizationService Service { get; private set; }

        private TraceRecorder trace;

        protected Common Common { get; private set; }

        /// <summary>The application user the tests run as.</summary>
        protected Guid UserId { get; private set; }

        protected Guid BusinessUnitId { get; private set; }

        public TestContext TestContext { get; set; }

        /// <summary>The environment variable with the connection string.</summary>
        protected abstract string ConnectionVariable { get; }

        [TestInitialize]
        public void ConnectToDataverse()
        {
            Service = DataverseConnection.Connect(ConnectionVariable);

            if (Service == null)
            {
                Assert.Inconclusive($"Set {ConnectionVariable} (tools\\Set-DataverseTestConnections.ps1) to run the integration tests.");
            }

            trace = new TraceRecorder();
            Common = new Common(Service, trace);

            var whoAmI = (WhoAmIResponse)Service.Execute(new WhoAmIRequest());
            UserId = whoAmI.UserId;
            BusinessUnitId = whoAmI.BusinessUnitId;
        }

        [TestCleanup]
        public void DeleteCreatedRecords()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                try
                {
                    Service.Delete(created[i].LogicalName, created[i].Id);
                }
                catch (Exception ex)
                {
                    TestContext?.WriteLine($"Could not delete {created[i].LogicalName} {created[i].Id}: {ex.Message}");
                }
            }

            created.Clear();

            foreach (var message in trace?.Messages ?? new List<string>())
            {
                TestContext?.WriteLine(message);
            }
        }

        /// <summary>Creates a record and deletes it again when the test ends.</summary>
        protected EntityReference Create(Entity entity)
        {
            var reference = new EntityReference(entity.LogicalName, Service.Create(entity));
            created.Add(reference);

            return reference;
        }

        /// <summary>Marks a record created another way (e.g. by the code under test) for deletion.</summary>
        protected void DeleteAfterTest(EntityReference record)
        {
            created.Add(record);
        }

        /// <summary>A unique name for a test record.</summary>
        protected static string UniqueName(string what)
        {
            return $"{Prefix} {what} {Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        /// <summary>A record URL like the ones workflows pass in (the host isn't used).</summary>
        protected static string UrlFor(EntityReference record)
        {
            return $"https://test.crm.dynamics.com/main.aspx?etn={record.LogicalName}&id={record.Id}&pagetype=entityrecord";
        }
    }

    [TestClass]
    [TestCategory("Integration")]
    public partial class Dynamics365_IntegrationTests : IntegrationTestBase
    {
        protected override string ConnectionVariable => "DATAVERSE_CONNECTION";
    }

    [TestClass]
    [TestCategory("Integration")]
    public class PowerPlatform_IntegrationTests : IntegrationTestBase
    {
        protected override string ConnectionVariable => "DATAVERSE_CONNECTION_PP";
    }
}
