using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;
using System.ServiceModel;

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

        [TestMethod]
        public void UpdateChildRecords_SetsTheConvertedValueOnEveryActiveChildAcrossPages()
        {
            var children = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            SetUpChildRelationship(new IntegerAttributeMetadata { LogicalName = "new_count" });
            service.OnRetrieveMultiple = query => ((QueryExpression)query).PageInfo.PageNumber == 1
                ? Page(true, "cookie1", new Entity(EntityNames.Contact, children[0]), new Entity(EntityNames.Contact, children[1]))
                : Page(false, null, new Entity(EntityNames.Contact, children[2]));

            var updated = common.UpdateChildRecords("account_contacts", EntityNames.Account, RecordId, string.Empty, "42", "new_count", true);

            Assert.AreEqual(3, updated);
            CollectionAssert.AreEqual(children, service.Updated.Select(e => e.Id).ToArray());
            Assert.IsTrue(service.Updated.All(e => e.LogicalName == EntityNames.Contact && (int)e["new_count"] == 42));
            var conditions = ((QueryExpression)service.Queries.First()).Criteria.Conditions;
            AssertCondition(conditions[0], "parentcustomerid", ConditionOperator.Equal, RecordId);
            AssertCondition(conditions[1], AttributeNames.StateCode, ConditionOperator.Equal, 0);
            Assert.AreEqual(1, service.Executed.OfType<RetrieveAttributeRequest>().Count());
        }

        [TestMethod]
        public void UpdateChildRecords_CanContinuePastALockedChild()
        {
            // upstream issue #269: a plugin stops updates to some children
            var children = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            SetUpChildRelationship(new IntegerAttributeMetadata { LogicalName = "new_count" });
            service.OnRetrieveMultiple = query => Collection(children.Select(id => new Entity(EntityNames.Contact, id)).ToArray());
            service.OnUpdate = entity =>
            {
                if (entity.Id == children[1])
                {
                    throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "The record is locked." });
                }
            };

            var updated = common.UpdateChildRecords("account_contacts", EntityNames.Account, RecordId, string.Empty, "42", "new_count", false, true, out var failed);

            Assert.AreEqual(2, updated);
            Assert.AreEqual(1, failed);
            CollectionAssert.AreEqual(new[] { children[0], children[2] }, service.Updated.Select(e => e.Id).ToArray());
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<OrganizationServiceFault>))]
        public void UpdateChildRecords_StopsAtAFailedChildByDefault()
        {
            SetUpChildRelationship(new IntegerAttributeMetadata { LogicalName = "new_count" });
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.Contact, Guid.NewGuid()));
            service.OnUpdate = entity => throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "The record is locked." });

            common.UpdateChildRecords("account_contacts", EntityNames.Account, RecordId, string.Empty, "42", "new_count", false);
        }

        [TestMethod]
        public void UpdateChildRecords_CopiesTheParentField()
        {
            var owner = new EntityReference(EntityNames.SystemUser, UserId);
            SetUpChildRelationship(new LookupAttributeMetadata { LogicalName = "new_reviewerid" });
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["ownerid"] = owner };
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.Contact, Guid.NewGuid()));

            common.UpdateChildRecords("account_contacts", EntityNames.Account, RecordId, "ownerid", null, "new_reviewerid", false);

            Assert.AreSame(owner, service.Updated.Single()["new_reviewerid"]);
            Assert.AreEqual(1, ((QueryExpression)service.Queries.Single()).Criteria.Conditions.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void UpdateChildRecords_ProductPropertiesThrow()
        {
            service.OnExecute = r => RelationshipResponse(EntityNames.DynamicPropertyInstance, "regardingobjectid");

            common.UpdateChildRecords("SalesOrderDetail_Dynamicpropertyinstance", "salesorderdetail", RecordId, string.Empty, "333", "valuestring", false);
        }

        [TestMethod]
        public void GetChildRecords_UsesTheRelationshipLookup()
        {
            service.OnExecute = r => RelationshipResponse(EntityNames.Contact, "parentcustomerid");

            common.GetChildRecords("account_contacts", RecordId);

            var query = (QueryByAttribute)service.Queries.Single();
            Assert.AreEqual(EntityNames.Contact, query.EntityName);
            Assert.AreEqual("parentcustomerid", query.Attributes.Single());
            Assert.AreEqual(RecordId, query.Values.Single());
        }

        private void SetUpChildRelationship(AttributeMetadata childField)
        {
            service.OnExecute = r => r is RetrieveRelationshipRequest
                ? (OrganizationResponse)RelationshipResponse(EntityNames.Contact, "parentcustomerid")
                : new RetrieveAttributeResponse { Results = { ["AttributeMetadata"] = childField } };
        }

        private static RetrieveRelationshipResponse RelationshipResponse(string childEntity, string lookup)
        {
            return new RetrieveRelationshipResponse
            {
                Results = { ["RelationshipMetadata"] = new OneToManyRelationshipMetadata { ReferencingEntity = childEntity, ReferencingAttribute = lookup } }
            };
        }

        [TestMethod]
        public void AssociateEntity_AssociatesUnlessAlreadyAssociated()
        {
            var roleId = Guid.NewGuid();

            common.AssociateEntity(EntityNames.Team, TeamId, "teamroles_association", EntityNames.TeamRoles, EntityNames.Role, roleId);

            var call = service.Associated.Single();
            Assert.AreEqual(new EntityReference(EntityNames.Team, TeamId), call.Record);
            Assert.AreEqual("teamroles_association", call.Relationship.SchemaName);
            Assert.AreEqual(roleId, call.Related.Single().Id);

            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.Team, TeamId));
            common.AssociateEntity(EntityNames.Team, TeamId, "teamroles_association", EntityNames.TeamRoles, EntityNames.Role, roleId);

            Assert.AreEqual(1, service.Associated.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AssociateEntity_LetsErrorsThrough()
        {
            service.OnRetrieveMultiple = query => throw new InvalidOperationException("unknown relationship");

            common.AssociateEntity(EntityNames.Team, TeamId, "nope", "nope", EntityNames.Role, Guid.NewGuid());
        }
    }
}
