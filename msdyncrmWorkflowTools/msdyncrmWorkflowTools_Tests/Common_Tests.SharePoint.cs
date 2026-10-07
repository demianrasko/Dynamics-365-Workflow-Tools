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
        public void GetAbsoluteUrlFromLocation_NoLocationIsNotFound()
        {
            Assert.AreEqual("URL Not found", common.GetAbsoluteUrlFromLocation(new EntityCollection()));
            Assert.AreEqual(0, service.Executed.Count);
        }

        [TestMethod]
        public void GetAbsoluteUrlFromLocation_ReturnsTheFirstLocationsUrl()
        {
            var location = new Entity("sharepointdocumentlocation", Guid.NewGuid());
            service.OnExecute = r => new RetrieveAbsoluteAndSiteCollectionUrlResponse { Results = { ["AbsoluteUrl"] = "https://sp/site/doc", ["SiteCollectionUrl"] = "https://sp" } };

            Assert.AreEqual("https://sp/site/doc", common.GetAbsoluteUrlFromLocation(Collection(location)));
            Assert.AreEqual(location.Id, ((RetrieveAbsoluteAndSiteCollectionUrlRequest)service.Executed.Single()).Target.Id);
        }

        [TestMethod]
        public void GetSharepointLocationUrl_FromARecordUrl()
        {
            service.OnRetrieveMultiple = q => Collection();

            Assert.AreEqual("URL Not found", common.GetSharepointLocationUrl(UrlFor(EntityNames.Account)));
            StringAssert.Contains(string.Join(" ", ((QueryExpression)service.Queries.Single()).Criteria.Conditions.SelectMany(c => c.Values)), RecordId.ToString());
            AssertRequired("Record URL", () => common.GetSharepointLocationUrl(null));
        }
    }
}
