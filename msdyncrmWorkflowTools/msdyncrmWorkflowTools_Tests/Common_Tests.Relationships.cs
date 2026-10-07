using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Collections.Generic;
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
        public void GetManyToManyRelatedIds_HandlesSwappedIntersectAttributes()
        {
            // accountleads_association's metadata pairs account with leadid and lead with accountid
            service.OnExecute = r => new RetrieveRelationshipResponse
            {
                Results =
                {
                    ["RelationshipMetadata"] = new ManyToManyRelationshipMetadata
                    {
                        Entity1LogicalName = "account", Entity1IntersectAttribute = "leadid",
                        Entity2LogicalName = "lead", Entity2IntersectAttribute = "accountid",
                        IntersectEntityName = "accountleads"
                    }
                }
            };

            common.GetManyToManyRelatedIds("accountleads_association", "account", IdA);
            common.GetManyToManyRelatedIds("accountleads_association", "lead", IdA);

            var fromAccount = (QueryExpression)service.Queries[0];
            Assert.AreEqual("lead", fromAccount.EntityName);
            Assert.AreEqual("leadid", fromAccount.LinkEntities.Single().LinkFromAttributeName);
            AssertCondition(fromAccount.LinkEntities.Single().LinkCriteria.Conditions.Single(), "accountid", ConditionOperator.Equal, IdA);

            var fromLead = (QueryExpression)service.Queries[1];
            Assert.AreEqual("account", fromLead.EntityName);
            Assert.AreEqual("accountid", fromLead.LinkEntities.Single().LinkFromAttributeName);
            AssertCondition(fromLead.LinkEntities.Single().LinkCriteria.Conditions.Single(), "leadid", ConditionOperator.Equal, IdA);
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

        [TestMethod]
        public void GetChildRecords_OnlyActive_AddsTheStatusCondition()
        {
            service.OnExecute = r => RelationshipResponse(EntityNames.Contact, "parentcustomerid");

            common.GetChildRecords("account_contacts", RecordId, onlyActive: true);

            var query = (QueryByAttribute)service.Queries.Single();
            CollectionAssert.AreEqual(new[] { "parentcustomerid", "statecode" }, query.Attributes.ToArray());
            CollectionAssert.AreEqual(new object[] { RecordId, 0 }, query.Values.ToArray());
        }

        [TestMethod]
        public void CloneChildren_CopiesTheSourceChildrenUnderTheTarget()
        {
            var child = Guid.NewGuid();
            var target = Guid.NewGuid();
            SetUpChildrenToClone(child);

            var count = common.CloneChildren($"https://x.crm.dynamics.com/main.aspx?etn=account&id={RecordId}",
                $"https://x.crm.dynamics.com/main.aspx?etn=account&id={{{target}}}", "account_contacts", "parentcustomerid", null, null, null, false, true);

            Assert.AreEqual(1, count);
            var query = (QueryByAttribute)service.Queries.Single();
            CollectionAssert.AreEqual(new object[] { RecordId, 0 }, query.Values.ToArray(), "the source's active children");
            var copy = service.Created.Single();
            Assert.AreEqual(new EntityReference(EntityNames.Account, target), copy["parentcustomerid"]);
            Assert.AreEqual(child.ToString(), copy["lastname"]);
        }

        [TestMethod]
        public void CloneChildren_ExplainsAMissingRequiredInput()
        {
            const string url = "https://x.crm.dynamics.com/main.aspx?etn=account&id=6b9c4a3e-1d2f-4e5a-8b7c-9d0e1f2a3b4c";

            AssertRequired("Relationship Name", () => common.CloneChildren(url, url, string.Empty, "parentcustomerid", null, null, null, false, false));
            AssertRequired("New Parent Field Name", () => common.CloneChildren(url, url, "account_contacts", null, null, null, null, false, false));
            AssertRequired("Source Record URL", () => common.CloneChildren(string.Empty, url, "account_contacts", "parentcustomerid", null, null, null, false, false));
            AssertRequired("Target Record URL", () => common.CloneChildren(url, null, "account_contacts", "parentcustomerid", null, null, null, false, false));
            Assert.AreEqual(0, service.Created.Count);
        }

        [TestMethod]
        public void CloneChildrenReplacements_ClearsADifferentOldParentField()
        {
            var parent = new EntityReference(EntityNames.Account, RecordId);

            var different = Common.CloneChildrenReplacements(parent, "new_targetid", "new_sourceid");
            Assert.AreEqual(parent, different["new_targetid"]);
            Assert.IsTrue(different.ContainsKey("new_sourceid") && different["new_sourceid"] == null);

            CollectionAssert.AreEqual(new[] { "parentcustomerid" }, Common.CloneChildrenReplacements(parent, "parentcustomerid", "parentcustomerid").Keys.ToArray(),
                "the same field isn't cleared");
            CollectionAssert.AreEqual(new[] { "parentcustomerid" }, Common.CloneChildrenReplacements(parent, "parentcustomerid", null).Keys.ToArray());
        }

        private void SetUpChildrenToClone(params Guid[] children)
        {
            service.OnExecute = r => r is RetrieveRelationshipRequest
                ? (OrganizationResponse)RelationshipResponse(EntityNames.Contact, "parentcustomerid")
                : new RetrieveEntityResponse { Results = { ["EntityMetadata"] = EntityWithAttributes("contactid", Attribute<StringAttributeMetadata>("lastname")) } };
            service.OnRetrieveMultiple = q => new EntityCollection(children.Select(id => new Entity(EntityNames.Contact, id)).ToList());
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["lastname"] = id.ToString() };
        }

        [TestMethod]
        public void CloneChildRecords_CopiesEachChildWithTheReplacements()
        {
            var children = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var newParent = new EntityReference(EntityNames.Account, Guid.NewGuid());
            SetUpChildrenToClone(children);

            var count = common.CloneChildRecords("account_contacts", RecordId, null, null, new Dictionary<string, object> { ["parentcustomerid"] = newParent }, false, false);

            Assert.AreEqual(2, count);
            CollectionAssert.AreEqual(children.Select(id => id.ToString()).ToArray(), service.Created.Select(c => (string)c["lastname"]).ToArray());
            Assert.IsTrue(service.Created.All(c => newParent.Equals(c["parentcustomerid"])));
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

        [TestMethod]
        public void IsAssociated_LooksForTheRecordsInTheIntersect()
        {
            service.OnExecute = r => new RetrieveRelationshipResponse
            {
                Results = { ["RelationshipMetadata"] = new ManyToManyRelationshipMetadata { IntersectEntityName = "accountleads" } }
            };
            service.OnRetrieveMultiple = q => Collection(new Entity("accountleads", Guid.NewGuid()));
            var account = new EntityReference(EntityNames.Account, Guid.NewGuid());

            Assert.IsTrue(common.IsAssociated(account, "accountleads_association", UrlFor("lead")));
            Assert.AreEqual("accountleads", ((QueryExpression)service.Queries.Single()).LinkEntities.Single().LinkToEntityName);

            service.OnRetrieveMultiple = q => Collection();
            Assert.IsFalse(common.IsAssociated(account, "accountleads_association", UrlFor("lead")));
            AssertRequired("Record URL", () => common.IsAssociated(account, "accountleads_association", null));
        }

        [TestMethod]
        public void AssociateEntity_FromARecordUrlIgnoresADuplicate()
        {
            var account = new EntityReference(EntityNames.Account, Guid.NewGuid());

            common.AssociateEntity(account, "accountleads_association", "accountleads", UrlFor("lead"));

            var call = service.Associated.Single();
            Assert.AreEqual(account, call.Record);
            Assert.AreEqual(RecordId, call.Related.Single().Id);

            service.OnAssociate = () => throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { ErrorCode = -2147220937 }, "duplicate");
            common.AssociateEntity(account, "accountleads_association", "accountleads", UrlFor("lead"));

            service.OnAssociate = () => throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { ErrorCode = -1 }, "other");
            Assert.ThrowsException<FaultException<OrganizationServiceFault>>(() => common.AssociateEntity(account, "accountleads_association", "accountleads", UrlFor("lead")));
            AssertRequired("Record URL", () => common.AssociateEntity(account, "accountleads_association", "accountleads", string.Empty));
        }

        [TestMethod]
        public void CountChildRecords_FromARecordUrl()
        {
            service.OnRetrieveMultiple = q => Page(false, null, new Entity(EntityNames.Contact, Guid.NewGuid()), new Entity(EntityNames.Contact, Guid.NewGuid()));

            Assert.AreEqual(2, common.CountChildRecords(EntityNames.Contact, "parentcustomerid", UrlFor(EntityNames.Account), null));
            AssertRequired("Record URL (Parent)", () => common.CountChildRecords(EntityNames.Contact, "parentcustomerid", (string)null, null));
        }

        [TestMethod]
        public void UpdateChildRecords_FromARecordUrl()
        {
            var owner = new EntityReference(EntityNames.SystemUser, UserId);
            SetUpChildRelationship(new LookupAttributeMetadata { LogicalName = "new_reviewerid" });
            service.OnRetrieve = (name, id, columns) => new Entity(name, id) { ["ownerid"] = owner };
            service.OnRetrieveMultiple = query => Collection(new Entity(EntityNames.Contact, Guid.NewGuid()));

            Assert.AreEqual(1, common.UpdateChildRecords(UrlFor(EntityNames.Account), "account_contacts", "ownerid", null, "new_reviewerid", false, false, out var failed));

            Assert.AreEqual(0, failed);
            Assert.AreSame(owner, service.Updated.Single()["new_reviewerid"]);
            AssertRequired("Parent Record URL", () => common.UpdateChildRecords(string.Empty, "account_contacts", "ownerid", null, "new_reviewerid", false, false, out _));
        }
    }
}
