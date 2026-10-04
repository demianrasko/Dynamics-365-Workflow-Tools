using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System.Linq;

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

        [TestMethod]
        public void ParseOptionSetValues_SkipsAndReportsInvalidValues()
        {
            var invalid = new System.Collections.Generic.List<string>();

            var values = Utility.ParseOptionSetValues("1, 3,x,7", invalid);

            CollectionAssert.AreEqual(new[] { 1, 3, 7 }, values.Select(v => v.Value).ToArray());
            CollectionAssert.AreEqual(new[] { "x" }, invalid);
        }

        [TestMethod]
        public void ParseOptionSetValues_EmptyReturnsEmptyCollection()
        {
            Assert.AreEqual(0, Utility.ParseOptionSetValues(string.Empty).Count);
        }

        [TestMethod]
        public void MergeOptionSetValues_KeepsExistingAndAddsNewWithoutDuplicates()
        {
            var existing = new OptionSetValueCollection { new OptionSetValue(1), new OptionSetValue(2) };
            var added = new OptionSetValueCollection { new OptionSetValue(2), new OptionSetValue(5) };

            var merged = Utility.MergeOptionSetValues(added, existing);

            CollectionAssert.AreEqual(new[] { 1, 2, 5 }, merged.Select(v => v.Value).ToArray());
        }

        [TestMethod]
        public void MergeOptionSetValues_HandlesNulls()
        {
            Assert.AreEqual(0, Utility.MergeOptionSetValues(null, null).Count);
            Assert.AreEqual(1, Utility.MergeOptionSetValues(new OptionSetValueCollection { new OptionSetValue(4) }, null).Count);
        }

        [TestMethod]
        public void GetFirstFetchAttributeKey_UsesNameAliasOrLinkAlias()
        {
            Assert.AreEqual("revenue", Utility.GetFirstFetchAttributeKey("<fetch><entity name='account'><attribute name='revenue' /><attribute name='name' /></entity></fetch>"));
            Assert.AreEqual("total", Utility.GetFirstFetchAttributeKey("<fetch><entity name='account'><attribute name='revenue' alias='total' /></entity></fetch>"));
            Assert.AreEqual("o.estimatedvalue", Utility.GetFirstFetchAttributeKey("<fetch><entity name='account'><link-entity name='opportunity' from='parentaccountid' to='accountid' alias='o'><attribute name='estimatedvalue' /></link-entity></entity></fetch>"));
            Assert.IsNull(Utility.GetFirstFetchAttributeKey("<fetch><entity name='account' /></fetch>"));
            Assert.IsNull(Utility.GetFirstFetchAttributeKey("not xml"));
        }

        [TestMethod]
        public void ToDecimal_ReadsNumbersMoneyAndAliasedValues()
        {
            Assert.AreEqual(5m, Utility.ToDecimal(5));
            Assert.AreEqual(2.5m, Utility.ToDecimal(new Money(2.5m)));
            Assert.AreEqual(7m, Utility.ToDecimal(new AliasedValue("opportunity", "estimatedvalue", new Money(7m))));
            Assert.IsNull(Utility.ToDecimal("text"));
            Assert.IsNull(Utility.ToDecimal(null));
        }

        [TestMethod]
        public void CalculateRollup_CountsEveryRecordAndIgnoresMissingValues()
        {
            var result = Utility.CalculateRollup(new decimal?[] { 4m, null, 10m, -2m });

            Assert.AreEqual(4m, result.Count);
            Assert.AreEqual(12m, result.Sum);
            Assert.AreEqual(4m, result.Average);
            Assert.AreEqual(-2m, result.Min);
            Assert.AreEqual(10m, result.Max);
        }

        [TestMethod]
        public void CalculateRollup_EmptyIsAllZero()
        {
            var result = Utility.CalculateRollup(new decimal?[0]);

            Assert.AreEqual(0m, result.Count);
            Assert.AreEqual(0m, result.Average);
            Assert.AreEqual(0m, result.Min);
        }

        [TestMethod]
        public void SplitAttributeNames_TrimsAndDropsEmptyEntries()
        {
            CollectionAssert.AreEqual(new[] { "new_colors", "new_sizes" }, Utility.SplitAttributeNames(" new_colors, ,new_sizes "));
            Assert.AreEqual(0, Utility.SplitAttributeNames(null).Length);
        }

        [TestMethod]
        public void GetMarketingListMember_PrefersAccountThenContactThenLead()
        {
            var account = new EntityReference("account", System.Guid.NewGuid());
            var contact = new EntityReference("contact", System.Guid.NewGuid());
            var lead = new EntityReference("lead", System.Guid.NewGuid());

            Assert.AreSame(account, Utility.GetMarketingListMember(account, contact, lead));
            Assert.AreSame(contact, Utility.GetMarketingListMember(null, contact, lead));
            Assert.AreSame(lead, Utility.GetMarketingListMember(null, null, lead));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void GetMarketingListMember_NoneSetThrows()
        {
            Utility.GetMarketingListMember(null, null, null);
        }

        [TestMethod]
        public void HasFetchTop_ReadsTheRootAttribute()
        {
            Assert.IsTrue(Utility.HasFetchTop("<fetch top='10'><entity name='account' /></fetch>"));
            Assert.IsFalse(Utility.HasFetchTop("<fetch><entity name='account'><attribute name='stop' /></entity></fetch>"));
            Assert.IsFalse(Utility.HasFetchTop("not xml"));
        }

        [TestMethod]
        public void FormatConcatenationValue_ConvertsLookupsMoneyChoicesAndAliases()
        {
            var record = new Entity("account")
            {
                ["parentaccountid"] = new EntityReference("account", System.Guid.NewGuid()) { Name = "Contoso" },
                ["revenue"] = new Money(12.5m),
                ["industrycode"] = new OptionSetValue(7),
                ["c.fullname"] = new AliasedValue("contact", "fullname", "Ana Silva")
            };
            record.FormattedValues["industrycode"] = "Retail";

            Assert.AreEqual("Contoso", Utility.FormatConcatenationValue(record, "parentaccountid", string.Empty));
            Assert.AreEqual("12.50", Utility.FormatConcatenationValue(record, "revenue", "F2"));
            Assert.AreEqual("Retail", Utility.FormatConcatenationValue(record, "industrycode", string.Empty));
            Assert.AreEqual("Ana Silva", Utility.FormatConcatenationValue(record, "c.fullname", string.Empty));
            Assert.IsNull(Utility.FormatConcatenationValue(record, "missing", string.Empty));
            Assert.AreEqual("Contoso", Utility.FormatConcatenationValue(record, null, string.Empty));
        }

        [TestMethod]
        public void GetFirstFetchValue_ReadsTheKeyOrTheFirstAttribute()
        {
            var created = new System.DateTime(2026, 1, 2);
            var record = new Entity("account") { ["accountid"] = System.Guid.NewGuid(), ["o.createdon"] = new AliasedValue("opportunity", "createdon", created) };

            Assert.AreEqual(created, Utility.GetFirstFetchValue(record, "o.createdon"));
            Assert.IsNull(Utility.GetFirstFetchValue(record, "missing"));
            Assert.AreEqual(record["accountid"], Utility.GetFirstFetchValue(record, null));
        }

        [TestMethod]
        public void Md5Hash_MatchesTheStandardTestVector()
        {
            Assert.AreEqual("900150983cd24fb0d6963f7d28e17f72", Utility.Md5Hash("abc"));
            Assert.AreEqual("d41d8cd98f00b204e9800998ecf8427e", Utility.Md5Hash(string.Empty));
        }

        [TestMethod]
        public void Sha512Hash_MatchesTheStandardTestVector()
        {
            Assert.AreEqual(
                "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f",
                Utility.Sha512Hash("abc"));
        }

        [TestMethod]
        public void GeocodeUrls_EscapeTheAddressAndUseHttps()
        {
            var bing = Utility.BuildBingGeocodeUrl(" 1 Main St #200 & Co/4 ", "bing key");
            var azure = Utility.BuildAzureMapsGeocodeUrl("1 Main St #200", "azure-key");

            Assert.AreEqual("https://dev.virtualearth.net/REST/v1/Locations?maxResults=1&query=1%20Main%20St%20%23200%20%26%20Co%2F4&key=bing%20key", bing);
            Assert.AreEqual("https://atlas.microsoft.com/geocode?api-version=2023-06-01&top=1&query=1%20Main%20St%20%23200&subscription-key=azure-key", azure);
        }

        [TestMethod]
        public void ParseBingGeocodeResponse_ReadsTheFirstGeocodePoint()
        {
            const string json = "{\"statusCode\":200,\"resourceSets\":[{\"estimatedTotal\":1,\"resources\":[{\"point\":{\"coordinates\":[1,2]},\"geocodePoints\":[{\"coordinates\":[47.640068,-122.129858]}]}]}]}";

            var location = Utility.ParseBingGeocodeResponse(json);

            Assert.AreEqual(47.640068m, location.Latitude);
            Assert.AreEqual(-122.129858m, location.Longitude);
        }

        [TestMethod]
        public void ParseBingGeocodeResponse_NoMatchIsNull()
        {
            Assert.IsNull(Utility.ParseBingGeocodeResponse("{\"statusCode\":200,\"resourceSets\":[{\"estimatedTotal\":0,\"resources\":[]}]}"));
        }

        [TestMethod]
        public void ParseBingGeocodeResponse_ErrorThrowsWithTheDetails()
        {
            try
            {
                Utility.ParseBingGeocodeResponse("{\"statusCode\":401,\"statusDescription\":\"Unauthorized\",\"errorDetails\":[\"Access was denied.\"]}");
                Assert.Fail("Expected InvalidPluginExecutionException");
            }
            catch (InvalidPluginExecutionException ex)
            {
                StringAssert.Contains(ex.Message, "401");
                StringAssert.Contains(ex.Message, "Access was denied.");
            }
        }

        [TestMethod]
        public void ParseAzureMapsGeocodeResponse_SwapsGeoJsonLongitudeLatitude()
        {
            const string json = "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[-122.138669,47.630359]}}]}";

            var location = Utility.ParseAzureMapsGeocodeResponse(json);

            Assert.AreEqual(47.630359m, location.Latitude);
            Assert.AreEqual(-122.138669m, location.Longitude);
        }

        [TestMethod]
        public void ParseAzureMapsGeocodeResponse_NoMatchIsNull()
        {
            Assert.IsNull(Utility.ParseAzureMapsGeocodeResponse("{\"type\":\"FeatureCollection\",\"features\":[]}"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseAzureMapsGeocodeResponse_ErrorThrows()
        {
            Utility.ParseAzureMapsGeocodeResponse("{\"error\":{\"code\":\"401 Unauthorized\",\"message\":\"Invalid subscription key.\"}}");
        }

        [TestMethod]
        public void GeocodeAddress_EmptyAddressIsNullWithoutARequest()
        {
            Assert.IsNull(Utility.GeocodeAddress("  ", "key"));
        }

        [TestMethod]
        public void JoinOptionSetValuesAndLabels()
        {
            var values = new OptionSetValueCollection { new OptionSetValue(1), new OptionSetValue(3), new OptionSetValue(9) };
            var labels = new System.Collections.Generic.Dictionary<int, string> { [1] = "Red", [3] = "Blue" };

            Assert.AreEqual("1,3,9", Utility.JoinOptionSetValues(values));
            Assert.AreEqual("Red,Blue,9", Utility.JoinOptionSetLabels(values, labels));
            Assert.AreEqual(string.Empty, Utility.JoinOptionSetValues(new OptionSetValueCollection()));
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

        [TestMethod]
        public void ParseCategories_TrimsAndRemovesEmptyAndDuplicateEntries()
        {
            CollectionAssert.AreEqual(new[] { "Billing", "support" }, Utility.ParseCategories(" Billing, ,support,Support , billing"));
            Assert.AreEqual(0, Utility.ParseCategories(null).Count);
        }

        [TestMethod]
        public void ConvertSettingValue_TypesNumbersAndBooleans()
        {
            Assert.AreEqual(42, Utility.ConvertSettingValue("42"));
            Assert.AreEqual(true, Utility.ConvertSettingValue("True"));
            Assert.AreEqual("abc", Utility.ConvertSettingValue("abc"));
        }
    }
}
