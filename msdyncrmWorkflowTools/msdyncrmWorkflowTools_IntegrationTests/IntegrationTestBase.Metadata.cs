using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void Metadata_TypeCodesAndRecordUrls()
        {
            Assert.AreEqual(1, Common.GetEntityTypeCode(EntityNames.Account));
            Assert.AreEqual(EntityNames.Contact, Common.ParseRecordUrl($"https://test.crm.dynamics.com/main.aspx?etc=2&id={Guid.NewGuid()}").EntityName);
        }

        [TestMethod]
        public void Metadata_EntityNamesAndRelationships()
        {
            Assert.AreEqual(EntityNames.Account, Common.GetEntityNameFromCode("1"));
            Assert.AreEqual(EntityNames.TeamMembership, Common.GetIntersectEntityName("teammembership_association"));
            Assert.AreEqual("not_a_relationship", Common.GetIntersectEntityName("not_a_relationship"));

            string primaryId = null;
            string primaryName = null;
            var attributes = Common.GetEntityAttributesToClone(EntityNames.Account, ref primaryId, ref primaryName);

            Assert.AreEqual("accountid", primaryId);
            Assert.AreEqual(AttributeNames.Name, primaryName);
            CollectionAssert.Contains(attributes, "telephone1");
        }

        [TestMethod]
        public void OptionSets_ReadValuesAndLabels()
        {
            var options = TestOptions(TestChoice);
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("choice") });

            Assert.AreEqual(0, Common.GetOptionSetValue(account, TestChoice));

            Service.Update(new Entity(account.LogicalName, account.Id) { [TestChoice] = new OptionSetValue(options[1]) });

            Assert.AreEqual(options[1], Common.GetOptionSetValue(account, TestChoice));
            // a subset: an option another test (or workflow 05) just deleted can show, without a label, until the table is published
            CollectionAssert.IsSubsetOf(new[] { "One", "Two", "Three" }, Common.GetOptionSetLabels(EntityNames.Account, TestChoice).Values.ToList());
        }

        [TestMethod]
        public void OptionSets_InsertAndDeleteAnOption()
        {
            var value = TestOptions(TestChoice).Max() + 10;

            Assert.IsTrue(Common.InsertOptionValue(false, TestChoice, EntityNames.Account, "WFT extra", value, 1033));

            try
            {
                Assert.IsTrue(HasOption(EntityNames.Account, TestChoice, value));
            }
            finally
            {
                Common.DeleteOptionValue(false, TestChoice, EntityNames.Account, value);

                // a deleted option stays in the published choices, without a label, until the table is published,
                // and OptionSets_ReadValuesAndLabels would count it
                Service.Execute(new PublishXmlRequest { ParameterXml = "<importexportxml><entities><entity>account</entity></entities></importexportxml>" });
            }

            Assert.IsTrue(LacksOption(EntityNames.Account, TestChoice, value));
        }
    }
}
