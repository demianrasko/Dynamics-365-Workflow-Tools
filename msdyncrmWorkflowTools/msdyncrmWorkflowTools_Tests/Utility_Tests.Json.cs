using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
        [TestMethod]
        public void SerializeEntity_ProducesValidJsonInTheExistingShape()
        {
            var id = System.Guid.NewGuid();
            var parentId = System.Guid.NewGuid();
            var record = new Entity("account", id);
            record["name"] = "Contoso \"East\"\r\nBranch \\ HQ";
            record["donotemail"] = true;
            record["industrycode"] = new OptionSetValue(7);
            record["revenue"] = new Money(1234.5m);
            record["numberofemployees"] = 42;
            record["parentaccountid"] = new EntityReference("Account", parentId) { Name = "Parent \"Co\"" };
            record["createdon"] = new System.DateTime(2026, 10, 4, 5, 30, 0, System.DateTimeKind.Utc);

            var json = Utility.SerializeEntity("account", "accountid", id, record,
                new[] { "name", "donotemail", "industrycode", "revenue", "numberofemployees", "parentaccountid", "createdon", "missingattribute" });

            var body = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JObject.Parse(json)["account"];

            Assert.AreEqual(id.ToString(), (string)body["accountid"]);
            Assert.AreEqual("Contoso \"East\"\r\nBranch \\ HQ", (string)body["name"]);
            Assert.AreEqual(true, (bool)body["donotemail"]);
            Assert.AreEqual(7, (int)body["industrycode"]);
            Assert.AreEqual(1234.5m, (decimal)body["revenue"]);
            Assert.AreEqual(42, (int)body["numberofemployees"]);
            Assert.AreEqual("account", (string)body["parentaccountid"]["typename"]);
            Assert.AreEqual(parentId.ToString(), (string)body["parentaccountid"]["id"]);
            Assert.AreEqual("Parent \"Co\"", (string)body["parentaccountid"]["name"]);
            StringAssert.Contains(json, "\"createdon\":\"2026-10-04T05:30:00.0000000Z\"");
            Assert.IsNull(body["missingattribute"]);
            StringAssert.StartsWith(json, "{\"account\":{\"accountid\":");
        }
    }
}
