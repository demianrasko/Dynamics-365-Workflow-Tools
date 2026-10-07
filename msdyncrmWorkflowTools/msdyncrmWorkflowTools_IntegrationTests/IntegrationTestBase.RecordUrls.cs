using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void GetRecordReference_ReadsTheRecordUrl()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("url") });

            var reference = Common.GetRecordReference(UrlFor(account).Replace("etn=account&", "etc=1&"));

            Assert.AreEqual(EntityNames.Account, reference.LogicalName);
            Assert.AreEqual(account.Id, reference.Id);
        }

        [TestMethod]
        public void AppModules_FindTheAppAndBuildItsRecordUrl()
        {
            var app = Service.RetrieveMultiple(new QueryExpression(EntityNames.AppModule)
            {
                ColumnSet = new ColumnSet(AttributeNames.UniqueName),
                TopCount = 1
            }).Entities.FirstOrDefault();

            if (app == null)
            {
                Assert.Inconclusive("This environment has no model-driven apps.");
            }

            var uniqueName = app.GetAttributeValue<string>(AttributeNames.UniqueName);

            Assert.AreEqual(app.Id.ToString(), Common.GetAppModuleId(uniqueName));
            Assert.AreEqual($"https://test.crm.dynamics.com/main.aspx?id=1&appid={app.Id}",
                Common.GetAppRecordUrl("https://test.crm.dynamics.com/main.aspx?id=1", uniqueName));
        }
    }
}
