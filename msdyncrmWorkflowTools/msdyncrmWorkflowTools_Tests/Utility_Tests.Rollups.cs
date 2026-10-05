using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Utility_Tests
    {
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
    }
}
