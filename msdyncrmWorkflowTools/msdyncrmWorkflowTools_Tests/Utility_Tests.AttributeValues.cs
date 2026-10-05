using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
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
        public void JoinOptionSetValuesAndLabels()
        {
            var values = new OptionSetValueCollection { new OptionSetValue(1), new OptionSetValue(3), new OptionSetValue(9) };
            var labels = new System.Collections.Generic.Dictionary<int, string> { [1] = "Red", [3] = "Blue" };

            Assert.AreEqual("1,3,9", Utility.JoinOptionSetValues(values));
            Assert.AreEqual("Red,Blue,9", Utility.JoinOptionSetLabels(values, labels));
            Assert.AreEqual(string.Empty, Utility.JoinOptionSetValues(new OptionSetValueCollection()));
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

        [TestMethod]
        public void BuildUserSettings_WritesOnlyTheSuppliedSettings()
        {
            var userId = Guid.NewGuid();

            var settings = Utility.BuildUserSettings(userId, 0, 0, 0, 0, 0, -1, null);

            CollectionAssert.AreEquivalent(new[] { "systemuserid" }, settings.Attributes.Keys.ToArray());
            Assert.AreEqual(userId, settings["systemuserid"]);
        }

        [TestMethod]
        public void BuildUserSettings_WritesEverySuppliedSetting()
        {
            var settings = Utility.BuildUserSettings(Guid.NewGuid(), 250, 2, 85, 1033, 1036, 0, true);

            Assert.AreEqual(250, settings["paginglimit"]);
            Assert.AreEqual(2, settings["advancedfindstartupmode"]);
            Assert.AreEqual(85, settings["timezonecode"]);
            Assert.AreEqual(1033, settings["helplanguageid"]);
            Assert.AreEqual(1036, settings["uilanguageid"]);
            Assert.AreEqual(0, settings["defaultcalendarview"]);
            Assert.AreEqual(true, settings["issendasallowed"]);
        }

        [TestMethod]
        public void BuildUserSettings_IgnoresOutOfRangeModes()
        {
            var settings = Utility.BuildUserSettings(Guid.NewGuid(), 0, 3, 0, 0, 0, 5, false);

            Assert.IsFalse(settings.Contains("advancedfindstartupmode"));
            Assert.IsFalse(settings.Contains("defaultcalendarview"));
            Assert.AreEqual(false, settings["issendasallowed"]);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void BuildUserSettings_RejectsAnInvalidPagingLimit()
        {
            Utility.BuildUserSettings(Guid.NewGuid(), 30, 0, 0, 0, 0, -1, false);
        }

        [TestMethod]
        public void ConvertToAttributeType_ConvertsTextToTheFieldType()
        {
            Assert.AreEqual(42, Utility.ConvertToAttributeType("42", new IntegerAttributeMetadata()));
            Assert.AreEqual(42L, Utility.ConvertToAttributeType("42", new BigIntAttributeMetadata()));
            Assert.AreEqual(12.5m, Utility.ConvertToAttributeType("12.5", new DecimalAttributeMetadata()));
            Assert.AreEqual(12.5, Utility.ConvertToAttributeType("12.5", new DoubleAttributeMetadata()));
            Assert.AreEqual(12.5m, ((Money)Utility.ConvertToAttributeType("12.5", new MoneyAttributeMetadata())).Value);
            Assert.AreEqual(new DateTime(2026, 10, 5), Utility.ConvertToAttributeType("2026-10-05", new DateTimeAttributeMetadata()));
            Assert.AreEqual(2, ((OptionSetValue)Utility.ConvertToAttributeType("2", new PicklistAttributeMetadata())).Value);
            Assert.AreEqual(2, ((OptionSetValue)Utility.ConvertToAttributeType("2", new StateAttributeMetadata())).Value);
            Assert.AreEqual(3, ((OptionSetValue)Utility.ConvertToAttributeType("3", new StatusAttributeMetadata())).Value);
            Assert.AreEqual("abc", Utility.ConvertToAttributeType("abc", new StringAttributeMetadata()));
        }

        [TestMethod]
        public void ConvertToAttributeType_YesNoAcceptsTrueAndOneAndTreatsTheRestAsNo()
        {
            var yesNo = new BooleanAttributeMetadata();

            Assert.AreEqual(true, Utility.ConvertToAttributeType("1", yesNo));
            Assert.AreEqual(true, Utility.ConvertToAttributeType("True", yesNo));
            Assert.AreEqual(true, Utility.ConvertToAttributeType(true, yesNo));
            Assert.AreEqual(false, Utility.ConvertToAttributeType("0", yesNo));
            Assert.AreEqual(false, Utility.ConvertToAttributeType(null, yesNo));
        }

        [TestMethod]
        public void ConvertToAttributeType_KeepsTypedValuesAndClearsOnEmpty()
        {
            var money = new Money(5m);
            var reference = new EntityReference(EntityNames.Account, Guid.NewGuid());

            Assert.AreSame(money, Utility.ConvertToAttributeType(money, new MoneyAttributeMetadata()));
            Assert.AreSame(reference, Utility.ConvertToAttributeType(reference, new LookupAttributeMetadata()));
            Assert.AreEqual(7, ((OptionSetValue)Utility.ConvertToAttributeType(new OptionSetValue(7), new PicklistAttributeMetadata())).Value);
            Assert.AreEqual(5, Utility.ConvertToAttributeType(new Money(5m), new IntegerAttributeMetadata()));
            Assert.IsNull(Utility.ConvertToAttributeType(string.Empty, new IntegerAttributeMetadata()));
            Assert.IsNull(Utility.ConvertToAttributeType(null, new PicklistAttributeMetadata()));
            Assert.AreEqual("Contoso", Utility.ConvertToAttributeType(new EntityReference(EntityNames.Account, Guid.NewGuid()) { Name = "Contoso" }, new StringAttributeMetadata()));
        }

        [TestMethod]
        public void ConvertToAttributeType_GuidBecomesALookupToItsOnlyTable()
        {
            var id = Guid.NewGuid();
            var lookup = new LookupAttributeMetadata { Targets = new[] { EntityNames.Account } };

            var reference = (EntityReference)Utility.ConvertToAttributeType($"{{{id}}}", lookup);

            Assert.AreEqual(EntityNames.Account, reference.LogicalName);
            Assert.AreEqual(id, reference.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ConvertToAttributeType_GuidForAMultiTableLookupThrows()
        {
            var customer = new LookupAttributeMetadata { Targets = new[] { EntityNames.Account, EntityNames.Contact } };

            Utility.ConvertToAttributeType(Guid.NewGuid().ToString(), customer);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ConvertToAttributeType_TextThatIsNotANumberThrows()
        {
            Utility.ConvertToAttributeType("abc", new IntegerAttributeMetadata { LogicalName = "numberofemployees" });
        }
    }
}
