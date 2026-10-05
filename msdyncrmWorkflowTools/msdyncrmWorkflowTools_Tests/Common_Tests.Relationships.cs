using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void Associations_JoinsThroughTheIntersectEntity()
        {
            var query = Common.AssociationsQuery("account", IdA, "new_account_contact", "contact", IdB);

            Assert.AreEqual("account", query.EntityName);
            Assert.IsTrue(query.Distinct);
            var intersect = query.LinkEntities.Single();
            Assert.AreEqual("new_account_contact", intersect.LinkToEntityName);
            Assert.AreEqual("accountid", intersect.LinkFromAttributeName);
            Assert.AreEqual("accountid", intersect.LinkToAttributeName);
            AssertCondition(intersect.LinkCriteria.Conditions.Single(), "accountid", ConditionOperator.Equal, IdA);

            var related = intersect.LinkEntities.Single();
            Assert.AreEqual("contact", related.LinkToEntityName);
            Assert.AreEqual("contactid", related.LinkFromAttributeName);
            Assert.AreEqual("ac", related.EntityAlias);
            AssertCondition(related.LinkCriteria.Conditions.Single(), "contactid", ConditionOperator.Equal, IdB);
        }

        [TestMethod]
        public void ChildRecords_And_OrganizationSetting()
        {
            var children = Common.ChildRecordsQuery("contact", "parentcustomerid", IdA);
            Assert.AreEqual("contact", children.EntityName);
            AssertCondition(children.Criteria.Conditions.Single(), "parentcustomerid", ConditionOperator.Equal, IdA);

            var setting = Common.OrganizationSettingQuery("maxuploadfilesize");
            Assert.AreEqual("organization", setting.EntityName);
            CollectionAssert.AreEqual(new[] { "maxuploadfilesize" }, setting.ColumnSet.Columns.ToArray());
        }

        [TestMethod]
        public void ManyToManyRelated_MatchesThePrimaryRecordOnTheIntersect()
        {
            var query = Common.ManyToManyRelatedQuery("contact", "contactid", "contactid", "new_account_contact", "accountid", IdA);

            Assert.AreEqual("contact", query.EntityName);
            var intersect = query.LinkEntities.Single();
            Assert.AreEqual("new_account_contact", intersect.LinkToEntityName);
            Assert.AreEqual("contactid", intersect.LinkFromAttributeName);
            Assert.AreEqual("contactid", intersect.LinkToAttributeName);
            AssertCondition(intersect.LinkCriteria.Conditions.Single(), "accountid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void ChildRecordsFetchXml_AddsTheUserFilterAndEscapesValues()
        {
            var fetch = Common.ChildRecordsFetchXmlQuery("contact", "parentcustomerid", IdA, "<condition attribute='lastname' operator='eq' value='O&apos;Brien' />");
            var xml = System.Xml.Linq.XElement.Parse(fetch);
            var conditions = xml.Element("entity").Element("filter").Elements("condition").ToList();

            Assert.AreEqual("contact", (string)xml.Element("entity").Attribute("name"));
            Assert.AreEqual(2, conditions.Count);
            Assert.AreEqual(IdA.ToString(), (string)conditions[0].Attribute("value"));
            Assert.AreEqual("O'Brien", (string)conditions[1].Attribute("value"));
        }

        [TestMethod]
        public void ChildRecordsFetchXml_WithoutFilter()
        {
            var xml = System.Xml.Linq.XElement.Parse(Common.ChildRecordsFetchXmlQuery("contact", "parentcustomerid", IdA, null));

            Assert.AreEqual(1, xml.Element("entity").Element("filter").Elements("condition").Count());
        }

        [TestMethod]
        public void CountChildRecords_WithoutAFilterCountsTheChildren()
        {
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.Contact, Guid.NewGuid()));

            Assert.AreEqual(1, common.CountChildRecords(EntityNames.Contact, "parentcustomerid", RecordId, string.Empty));
            Assert.AreEqual(EntityNames.Contact, ((QueryExpression)service.Queries.Single()).EntityName);
            Assert.AreEqual(0, service.Executed.Count);
        }
    }
}
