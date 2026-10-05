using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// SharePoint document locations. Only Dataverse records are created (no folder is made in SharePoint); the
    /// environment needs a SharePoint site record, otherwise the test is inconclusive.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void SharePoint_FindsTheRecordsLocationAndItsUrl()
        {
            var site = Service.RetrieveMultiple(new QueryExpression("sharepointsite")
            {
                ColumnSet = new ColumnSet(AttributeNames.AbsoluteUrl),
                TopCount = 1
            }).Entities.FirstOrDefault();

            if (site == null)
            {
                Assert.Inconclusive("This environment has no SharePoint site.");
            }

            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("sharepoint") });
            var folder = $"WFT-Test-{Guid.NewGuid():N}";

            Assert.AreEqual("URL Not found", Common.GetAbsoluteUrlFromLocation(Common.GetSharepointLocations(account.Id)));

            Create(new Entity(EntityNames.SharePointDocumentLocation)
            {
                [AttributeNames.Name] = UniqueName("location"),
                [AttributeNames.RelativeUrl] = folder,
                ["parentsiteorlocation"] = site.ToEntityReference(),
                [AttributeNames.RegardingObjectId] = account
            });

            var locations = Common.GetSharepointLocations(account.Id);
            Assert.AreEqual(1, locations.Entities.Count);

            var url = Common.GetAbsoluteUrlFromLocation(locations);
            TestContext.WriteLine(url);
            StringAssert.EndsWith(url.TrimEnd('/'), folder);
        }
    }
}
