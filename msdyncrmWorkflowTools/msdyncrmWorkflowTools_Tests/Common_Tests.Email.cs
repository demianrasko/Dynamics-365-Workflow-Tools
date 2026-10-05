using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools_Tests
{
    public partial class Common_Tests
    {
        [TestMethod]
        public void SalesLiteratureToEmail_AttachesEachItemToTheEmail()
        {
            var emailId = Guid.NewGuid();
            service.OnRetrieveMultiple = query => Collection(
                new Entity("salesliteratureitem") { ["title"] = "Brochure", ["filename"] = "brochure.pdf", ["documentbody"] = "QUJD", ["mimetype"] = "application/pdf" },
                new Entity("salesliteratureitem") { ["filename"] = "price list.xlsx" });

            common.SalesLiteratureToEmail("*", Guid.NewGuid(), emailId);

            Assert.AreEqual(2, service.Created.Count);
            Assert.IsTrue(service.Created.All(a => a.LogicalName == "activitymimeattachment" && a.GetAttributeValue<EntityReference>("objectid").Id == emailId));
            CollectionAssert.AreEqual(new[] { 1, 2 }, service.Created.Select(a => a.GetAttributeValue<int>("attachmentnumber")).ToArray());
            Assert.AreEqual("Brochure", service.Created[0]["subject"]);
            Assert.AreEqual("QUJD", service.Created[0]["body"]);
            Assert.IsFalse(service.Created[1].Contains("subject"));
        }

        [TestMethod]
        public void SalesLiteratureToEmail_FiltersOnTheFileNamePattern()
        {
            var salesLiteratureId = Guid.NewGuid();

            common.SalesLiteratureToEmail("price*", salesLiteratureId, Guid.NewGuid());

            var query = (QueryExpression)service.Queries.Single();
            var conditions = query.Criteria.Conditions.Concat(query.LinkEntities.SelectMany(l => l.LinkCriteria.Conditions)).ToList();
            Assert.IsTrue(conditions.Any(c => c.Values.Contains("%price%%")), "file name pattern");
            Assert.IsTrue(conditions.Any(c => c.Values.Contains(salesLiteratureId)), "sales literature id");
            Assert.AreEqual(0, service.Created.Count, "no items, nothing attached");
        }

        [TestMethod]
        public void AddressEmailToTeam_SetsEveryMemberAsRecipient()
        {
            var emailId = Guid.NewGuid();
            var members = new[] { Guid.NewGuid(), Guid.NewGuid() };
            service.OnRetrieveMultiple = query => Collection(members.Select(id => new Entity("systemuser", id)).ToArray());

            Assert.AreEqual(2, common.AddressEmailToTeam(emailId, TeamId));

            var email = service.Updated.Single();
            Assert.AreEqual(emailId, email.Id);
            CollectionAssert.AreEqual(members, email.GetAttributeValue<EntityCollection>("to").Entities.Select(p => p.GetAttributeValue<EntityReference>("partyid").Id).ToArray());
        }

        [TestMethod]
        public void AddressEmailToTeam_EmptyTeamLeavesTheEmail()
        {
            Assert.AreEqual(0, common.AddressEmailToTeam(Guid.NewGuid(), TeamId));
            Assert.AreEqual(0, service.Updated.Count);
        }

        [TestMethod]
        public void SendEmailToUsersInRole_AddressesAndSends()
        {
            var emailId = Guid.NewGuid();
            service.OnRetrieveMultiple = query => Collection(new Entity("systemuser", UserId));
            service.OnExecute = r => new OrganizationResponse();

            common.SendEmailToUsersInRole(new EntityReference("role", Guid.NewGuid()), new EntityReference("email", emailId));

            Assert.AreEqual(UserId, service.Updated.Single().GetAttributeValue<EntityCollection>("to").Entities.Single().GetAttributeValue<EntityReference>("partyid").Id);
            Assert.AreEqual(emailId, ((SendEmailRequest)service.Executed.Single()).EmailId);
        }

        [TestMethod]
        public void UsersInRole_FiltersEnabledUsersByRole()
        {
            var query = Common.UsersInRoleQuery(IdA);

            Assert.AreEqual("systemuser", query.EntityName);
            AssertCondition(query.Criteria.Conditions.Single(), "accessmode", ConditionOperator.Equal, 0);
            var role = query.LinkEntities.Single().LinkEntities.Single();
            Assert.AreEqual("role", role.LinkToEntityName);
            AssertCondition(role.LinkCriteria.Conditions.Single(), "roleid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void SalesLiteratureItems_PassesTheFileNamePatternUnescaped()
        {
            var query = Common.SalesLiteratureItemsQuery("%O'Brien & Co%", IdA);

            AssertCondition(query.Criteria.Conditions[0], "filename", ConditionOperator.Like, "%O'Brien & Co%");
            AssertCondition(query.Criteria.Conditions[1], "salesliteratureid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void EntityAttachments_Notes()
        {
            var query = Common.EntityAttachmentsQuery(false, "%.pdf", IdA, 3);

            Assert.AreEqual("annotation", query.EntityName);
            Assert.AreEqual(3, query.TopCount);
            Assert.AreEqual(OrderType.Descending, query.Orders.Single().OrderType);
            AssertCondition(query.Criteria.Conditions[0], "isdocument", ConditionOperator.Equal, true);
            AssertCondition(query.Criteria.Conditions[1], "objectid", ConditionOperator.Equal, IdA);
            AssertCondition(query.Criteria.Conditions[2], "filename", ConditionOperator.Like, "%.pdf");
        }

        [TestMethod]
        public void EntityAttachments_EmailAttachmentsWithoutFilterOrTop()
        {
            var query = Common.EntityAttachmentsQuery(true, null, IdA, 0);

            Assert.AreEqual("activitymimeattachment", query.EntityName);
            Assert.IsNull(query.TopCount);
            AssertCondition(query.Criteria.Conditions.Single(), "activityid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void TeamMembers_FiltersOnTheTeam()
        {
            var query = Common.TeamMembersQuery(IdA);

            Assert.AreEqual("systemuser", query.EntityName);
            Assert.AreEqual("teammembership", query.LinkEntities.Single().LinkToEntityName);
            AssertCondition(query.LinkEntities.Single().LinkCriteria.Conditions.Single(), "teamid", ConditionOperator.Equal, IdA);
        }

        [TestMethod]
        public void EntityAttachmentsQuery_SeveralPatternsMatchAnyOfThem()
        {
            var query = Common.EntityAttachmentsQuery(false, "%.pdf; %.docx;", IdA, 0);

            var anyPattern = query.Criteria.Filters.Single();
            Assert.AreEqual(LogicalOperator.Or, anyPattern.FilterOperator);
            AssertCondition(anyPattern.Conditions[0], AttributeNames.FileName, ConditionOperator.Like, "%.pdf");
            AssertCondition(anyPattern.Conditions[1], AttributeNames.FileName, ConditionOperator.Like, "%.docx");
            Assert.IsFalse(query.Criteria.Conditions.Any(c => c.AttributeName == AttributeNames.FileName));
        }
    }
}
