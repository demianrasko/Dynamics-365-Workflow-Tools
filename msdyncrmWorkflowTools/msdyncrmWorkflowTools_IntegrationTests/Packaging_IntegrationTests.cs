using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Imports the unmanaged Power Platform solution built by tools\Build-Solutions.ps1 into the Power Platform test
    /// environment, proving Dataverse accepts the package. The managed file isn't imported: it would make the
    /// assembly managed there, and the deployment test (and real test workflows) use an updatable assembly.
    /// </summary>
    [TestClass]
    [TestCategory("Packaging")]
    public class Packaging_IntegrationTests
    {
        private const string ConnectionVariable = "DATAVERSE_CONNECTION_PP";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PowerPlatformSolution_Imports()
        {
            var service = DataverseConnection.Connect(ConnectionVariable);

            if (service == null)
            {
                Assert.Inconclusive($"Set {ConnectionVariable} (tools\\Set-DataverseTestConnections.ps1) to run the packaging test.");
            }

            var dist = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\dist"));
            var zip = Directory.Exists(dist)
                ? Directory.GetFiles(dist, "PowerPlatformWorkflowTools_*.zip").Where(f => !f.EndsWith("_managed.zip")).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                : null;

            if (zip == null)
            {
                Assert.Inconclusive("Build the solution files first: tools\\Build-Solutions.ps1");
            }

            TestContext.WriteLine($"Importing {Path.GetFileName(zip)}");

            // imported in the background and waited for: a synchronous import can outlast the connection
            var import = (ImportSolutionAsyncResponse)service.Execute(new ImportSolutionAsyncRequest
            {
                CustomizationFile = File.ReadAllBytes(zip),
                OverwriteUnmanagedCustomizations = true,
                PublishWorkflows = false
            });

            var finished = false;

            for (var attempt = 0; attempt < 80 && !finished; attempt++)
            {
                Thread.Sleep(15000);
                var operation = service.Retrieve("asyncoperation", import.AsyncOperationId, new ColumnSet("statecode", "statuscode", "message"));

                if (operation.GetAttributeValue<OptionSetValue>("statecode").Value == 3)
                {
                    finished = true;
                    Assert.AreEqual(30, operation.GetAttributeValue<OptionSetValue>("statuscode").Value,
                        $"The import failed: {operation.GetAttributeValue<string>("message")}");
                }
            }

            Assert.IsTrue(finished, "The import didn't finish within 20 minutes.");

            var version = Path.GetFileNameWithoutExtension(zip).Substring("PowerPlatformWorkflowTools_".Length).Replace('_', '.');
            var solution = service.RetrieveMultiple(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("version", "ismanaged"),
                Criteria = { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, "PowerPlatformWorkflowTools") } }
            }).Entities.Single();

            Assert.AreEqual(version, solution.GetAttributeValue<string>("version"));
            Assert.IsFalse(solution.GetAttributeValue<bool>("ismanaged"));

            var assembly = service.RetrieveMultiple(new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("version"),
                Criteria = { Conditions = { new ConditionExpression("name", ConditionOperator.Equal, "powerplatformWorkflowTools") } }
            }).Entities.Single();

            Assert.AreEqual(version, assembly.GetAttributeValue<string>("version"));
            Assert.AreEqual(1, service.RetrieveMultiple(new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet(false),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solution.Id),
                        new ConditionExpression("objectid", ConditionOperator.Equal, assembly.Id)
                    }
                }
            }).Entities.Count);
        }
    }
}
