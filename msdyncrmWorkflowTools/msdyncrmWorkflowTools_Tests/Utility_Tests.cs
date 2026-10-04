using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class Utility_Tests
    {
        private const string RecordUrl = "https://demianrasko.crm4.dynamics.com:443/main.aspx?etc=4207&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&histKey=885118818&newWindow=true&pagetype=entityrecord";

        [TestMethod]
        public void ParseRecordUrl_ReturnsTypeCodeAndId()
        {
            var parsedUrl = Utility.ParseRecordUrl(RecordUrl);

            Assert.AreEqual("4207", parsedUrl.ObjectTypeCode);
            Assert.AreEqual("d3c3b3b2-ae19-e811-811f-5065f38a3a01", parsedUrl.Id);
        }

        [TestMethod]
        public void ParseRecordUrl_FindsParametersInAnyOrder()
        {
            var parsedUrl = Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?pagetype=entityrecord&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&etc=1");

            Assert.AreEqual("1", parsedUrl.ObjectTypeCode);
            Assert.AreEqual("d3c3b3b2-ae19-e811-811f-5065f38a3a01", parsedUrl.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseRecordUrl_ThrowsWhenThereIsNoQueryString()
        {
            Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseRecordUrl_ThrowsWhenEmpty()
        {
            Utility.ParseRecordUrl(string.Empty);
        }

        [TestMethod]
        public void GetMask_NoFlags_ReturnsNone()
        {
            Assert.AreEqual(AccessRights.None, Utility.GetMask(false, false, false, false, false, false, false));
        }

        [TestMethod]
        public void GetMask_CombinesSelectedFlags()
        {
            var mask = Utility.GetMask(read: true, write: true, append: false, appendTo: false, delete: false, share: true, assign: false);

            Assert.AreEqual(AccessRights.ReadAccess | AccessRights.WriteAccess | AccessRights.ShareAccess, mask);
        }

        [TestMethod]
        public void GetMask_AllFlags()
        {
            var mask = Utility.GetMask(true, true, true, true, true, true, true);

            Assert.AreEqual(
                AccessRights.ReadAccess | AccessRights.WriteAccess | AccessRights.AppendAccess | AccessRights.AppendToAccess |
                AccessRights.DeleteAccess | AccessRights.ShareAccess | AccessRights.AssignAccess,
                mask);
        }

        [TestMethod]
        public void CreateXml_AddsPagingAttributes()
        {
            var xml = Utility.CreateXml("<fetch><entity name='account' /></fetch>", "cookie-value", 2, 250);

            Assert.AreEqual("<fetch paging-cookie=\"cookie-value\" page=\"2\" count=\"250\"><entity name=\"account\" /></fetch>", xml);
        }

        [TestMethod]
        public void CreateXml_LeavesOutUnsetAttributes()
        {
            var xml = Utility.CreateXml("<fetch><entity name='account' /></fetch>", null, 0, 0);

            Assert.AreEqual("<fetch><entity name=\"account\" /></fetch>", xml);
        }

        [TestMethod]
        public void GetParticipation_MapsKnownAttributes()
        {
            Assert.AreEqual("1", Utility.GetParticipation("from"));
            Assert.AreEqual("2", Utility.GetParticipation("to"));
            Assert.AreEqual("5", Utility.GetParticipation("requiredattendees"));
            Assert.AreEqual("11", Utility.GetParticipation("customer"));
        }

        [TestMethod]
        public void GetParticipation_UnknownAttributeReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, Utility.GetParticipation("subject"));
        }

        [TestMethod]
        public void HandleExceptions_NullReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, Utility.HandleExceptions(null));
        }

        [TestMethod]
        public void HandleExceptions_IncludesOrganizationServiceFaultDetails()
        {
            var fault = new OrganizationServiceFault
            {
                ErrorCode = -2147220891,
                Message = "Record is locked",
                TraceText = "plugin trace",
                InnerFault = new OrganizationServiceFault { ErrorCode = -2147220970, Message = "inner fault message" }
            };
            var ex = new System.ServiceModel.FaultException<OrganizationServiceFault>(fault, new System.ServiceModel.FaultReason("Record is locked"));

            var text = Utility.HandleExceptions(ex);

            StringAssert.Contains(text, "Code:\t-2147220891");
            StringAssert.Contains(text, "Fault Message:\tRecord is locked");
            StringAssert.Contains(text, "Trace:\tplugin trace");
            StringAssert.Contains(text, "--- Inner fault ---");
            StringAssert.Contains(text, "Code:\t-2147220970");
        }

        [TestMethod]
        public void HandleExceptions_WalksInnerExceptionsOfAnyType()
        {
            var ex = new InvalidPluginExecutionException("outer", new System.InvalidOperationException("middle", new System.ArgumentException("innermost")));

            var text = Utility.HandleExceptions(ex);

            StringAssert.Contains(text, "Message:\touter");
            StringAssert.Contains(text, "Message:\tmiddle");
            StringAssert.Contains(text, "Message:\tinnermost");
            StringAssert.Contains(text, "Type:\tSystem.ArgumentException");
        }

        [TestMethod]
        public void AttributeValueToString_ConvertsDataverseTypes()
        {
            var id = System.Guid.NewGuid();

            Assert.IsNull(Utility.AttributeValueToString(null));
            Assert.AreEqual("3", Utility.AttributeValueToString(new OptionSetValue(3)));
            Assert.AreEqual(id.ToString(), Utility.AttributeValueToString(new EntityReference("account", id)));
            Assert.AreEqual(12.5m.ToString(), Utility.AttributeValueToString(new Money(12.5m)));
            Assert.AreEqual("1,2", Utility.AttributeValueToString(new OptionSetValueCollection { new OptionSetValue(1), new OptionSetValue(2) }));
            Assert.AreEqual("7", Utility.AttributeValueToString(new AliasedValue("account", "numberofemployees", 7)));
            Assert.AreEqual("plain text", Utility.AttributeValueToString("plain text"));
        }

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

            var json = Utility.SerializeEntity("account", "accountid", id.ToString(), record,
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

        [TestMethod]
        public void CopyAttributeValue_CopiesToTheTargetAttribute()
        {
            var source = new Entity("salesliteratureitem") { ["title"] = "Brochure" };
            var target = new Entity("activitymimeattachment");

            Assert.IsTrue(Utility.CopyAttributeValue(source, "title", target, "subject"));
            Assert.AreEqual("Brochure", target["subject"]);
        }

        [TestMethod]
        public void CopyAttributeValue_DefaultsToTheSourceAttributeName()
        {
            var source = new Entity("annotation") { ["mimetype"] = "application/pdf" };
            var target = new Entity("activitymimeattachment");

            Assert.IsTrue(Utility.CopyAttributeValue(source, "mimetype", target));
            Assert.AreEqual("application/pdf", target["mimetype"]);
        }

        [TestMethod]
        public void CopyAttributeValue_MissingSourceLeavesTargetUnset()
        {
            var source = new Entity("annotation");
            var target = new Entity("activitymimeattachment");

            Assert.IsFalse(Utility.CopyAttributeValue(source, "documentbody", target, "body"));
            Assert.IsFalse(target.Contains("body"));
        }
    }
}
