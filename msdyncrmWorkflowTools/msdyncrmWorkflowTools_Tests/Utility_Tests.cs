using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public partial class Utility_Tests
    {
        private const string RecordUrl = "https://demianrasko.crm4.dynamics.com:443/main.aspx?etc=4207&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&histKey=885118818&newWindow=true&pagetype=entityrecord";

        [TestMethod]
        public void ParseRecordUrl_ReturnsTypeCodeAndId()
        {
            var parsedUrl = Utility.ParseRecordUrl(RecordUrl);

            Assert.AreEqual("4207", parsedUrl.ObjectTypeCode);
            Assert.AreEqual(new System.Guid("d3c3b3b2-ae19-e811-811f-5065f38a3a01"), parsedUrl.Id);
        }

        [TestMethod]
        public void ParseRecordUrl_FindsParametersInAnyOrder()
        {
            var parsedUrl = Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?pagetype=entityrecord&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&etc=1");

            Assert.AreEqual("1", parsedUrl.ObjectTypeCode);
            Assert.AreEqual(new System.Guid("d3c3b3b2-ae19-e811-811f-5065f38a3a01"), parsedUrl.Id);
        }

        [TestMethod]
        public void ParseRecordUrl_ReadsTheEntityNameFromEtn()
        {
            var parsedUrl = Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?etc=1&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&etn=account&pagetype=entityrecord");

            Assert.AreEqual("account", parsedUrl.EntityName);
            Assert.AreEqual("1", parsedUrl.ObjectTypeCode);
        }

        [TestMethod]
        public void ParseRecordUrl_LeavesEntityNameNullWithoutEtn()
        {
            Assert.IsNull(Utility.ParseRecordUrl(RecordUrl).EntityName);
        }

        [TestMethod]
        public void ParseRecordUrl_AcceptsBracedAndEncodedIds()
        {
            var expected = new System.Guid("d3c3b3b2-ae19-e811-811f-5065f38a3a01");

            Assert.AreEqual(expected, Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?etc=1&id={D3C3B3B2-AE19-E811-811F-5065F38A3A01}").Id);
            Assert.AreEqual(expected, Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?etc=1&id=%7bd3c3b3b2-ae19-e811-811f-5065f38a3a01%7d").Id);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseRecordUrl_ThrowsWhenTheIdIsNotAGuid()
        {
            Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?etc=1&id=not-a-guid");
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
        public void TryGetParticipation_MapsKnownAttributes()
        {
            var expected = new Dictionary<string, int> { ["from"] = 1, ["to"] = 2, ["requiredattendees"] = 5, ["customer"] = 11 };

            foreach (var attribute in expected.Keys)
            {
                Assert.IsTrue(Utility.GetParticipation(attribute, out var mask), attribute);
                Assert.AreEqual(expected[attribute], mask, attribute);
            }
        }

        [TestMethod]
        public void TryGetParticipation_UnknownAttributeReturnsFalse()
        {
            Assert.IsFalse(Utility.GetParticipation("subject", out _));
            Assert.IsFalse(Utility.GetParticipation(null, out _));
        }

        [TestMethod]
        public void BuildRecordUrl_ReusesTheAddressAndRoundTrips()
        {
            var id = new System.Guid("d3c3b3b2-ae19-e811-811f-5065f38a3a01");

            var url = Utility.BuildRecordUrl(RecordUrl, 2, "contact", id);
            var parsed = Utility.ParseRecordUrl(url);

            Assert.AreEqual("https://demianrasko.crm4.dynamics.com:443/main.aspx?etc=2&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&etn=contact&pagetype=entityrecord", url);
            Assert.AreEqual("2", parsed.ObjectTypeCode);
            Assert.AreEqual(id, parsed.Id);
            Assert.AreEqual("contact", parsed.EntityName);
        }
    }
}
