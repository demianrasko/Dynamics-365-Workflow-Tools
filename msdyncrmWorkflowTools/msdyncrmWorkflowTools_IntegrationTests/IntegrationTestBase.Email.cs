using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Linq;
using System.Text;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Email tests. They only ever address the application user, contacts with @example.com addresses (a reserved
    /// domain nothing is delivered to) and a new security role nobody else has, so no real person gets an email.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        [TestMethod]
        public void AddressEmailToTeam_AddressesTheMembers()
        {
            var email = CreateEmail(null);
            var team = CreateTeam();

            Assert.AreEqual(0, Common.AddressEmailToTeam(email.Id, team.Id));

            Common.AddTeamMember(team.Id, UserId);

            Assert.AreEqual(1, Common.AddressEmailToTeam(email.Id, team.Id));
            CollectionAssert.AreEqual(new[] { UserId }, Recipients(email));
        }

        [TestMethod]
        public void SetEmailRecipients_ReplacesTheToLine()
        {
            var email = CreateEmail(CreateExampleContact());

            Common.SetEmailRecipients(email.Id, new[] { UserId });

            CollectionAssert.AreEqual(new[] { UserId }, Recipients(email));
        }

        [TestMethod]
        public void SendEmail_SendsTheEmail()
        {
            var subject = UniqueName("send");
            var email = CreateEmail(CreateExampleContact(), subject);

            // the returned subject may end with the environment's tracking token (e.g. "CRM:0251001")
            StringAssert.StartsWith(Common.SendEmail(email.Id), subject);
            Assert.AreEqual(1, StateOf(email));
        }

        [TestMethod]
        public void SendEmailToUsersInRole_ARoleNobodyHasFailsClearly()
        {
            var role = CreateRole();
            var email = CreateEmail(CreateExampleContact());

            var error = Assert.ThrowsException<InvalidPluginExecutionException>(() => Common.SendEmailToUsersInRole(role, email));

            StringAssert.Contains(error.Message, "No enabled user has the security role");
            Assert.AreEqual(0, StateOf(email));
        }

        [TestMethod]
        public void EmailTemplates_SendToTheUserAndToARoleNobodyHas()
        {
            var template = CreateTemplate();

            Common.SendEmailFromTemplate(template, UserId);
            Assert.IsTrue(Common.SendEmailFromTemplateToUsersInRole(CreateRole(), template));

            var sent = Service.RetrieveMultiple(new QueryExpression(EntityNames.Email)
            {
                ColumnSet = new ColumnSet(AttributeNames.Subject),
                // the subject may end with the environment's tracking token
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.Subject, ConditionOperator.BeginsWith, TemplateSubject) } }
            }).Entities;

            foreach (var email in sent)
            {
                DeleteAfterTest(email.ToEntityReference());
            }

            Assert.AreEqual(1, sent.Count);
        }

        [TestMethod]
        public void EntityAttachmentToEmail_CopiesNotesAndEmailAttachments()
        {
            var account = Create(new Entity(EntityNames.Account) { [AttributeNames.Name] = UniqueName("attachments") });
            CreateNote(account, "first.txt");
            CreateNote(account, "second.pdf");
            CreateNote(account, "first.txt");

            var email = CreateEmail(null);
            Common.EntityAttachmentToEmail("%.txt", account.Id, email, false, false);
            CollectionAssert.AreEquivalent(new[] { "first.txt", "first.txt" }, AttachmentNames(email));

            var distinct = CreateEmail(null);
            Common.EntityAttachmentToEmail(string.Empty, account.Id, distinct, false, true);
            CollectionAssert.AreEquivalent(new[] { "first.txt", "second.pdf" }, AttachmentNames(distinct));

            var copy = CreateEmail(null);
            Common.EntityAttachmentToEmail(string.Empty, distinct.Id, copy, true, false);
            CollectionAssert.AreEquivalent(new[] { "first.txt", "second.pdf" }, AttachmentNames(copy));
        }

        private string TemplateSubject { get; } = UniqueName("template email");

        private EntityReference CreateExampleContact()
        {
            return Create(new Entity(EntityNames.Contact)
            {
                ["lastname"] = UniqueName("recipient"),
                ["emailaddress1"] = $"wft-test-{Guid.NewGuid():N}@example.com"
            });
        }

        private EntityReference CreateEmail(EntityReference to, string subject = null)
        {
            var email = new Entity(EntityNames.Email) { [AttributeNames.Subject] = subject ?? UniqueName("email") };

            if (to != null)
            {
                email[AttributeNames.To] = new EntityCollection { Entities = { new Entity(EntityNames.ActivityParty) { [AttributeNames.PartyId] = to } } };
            }

            return Create(email);
        }

        private Guid[] Recipients(EntityReference email)
        {
            return (Read(email, AttributeNames.To).GetAttributeValue<EntityCollection>(AttributeNames.To)?.Entities.AsEnumerable() ?? Enumerable.Empty<Entity>())
                .Select(p => p.GetAttributeValue<EntityReference>(AttributeNames.PartyId).Id)
                .ToArray();
        }

        private EntityReference CreateRole()
        {
            return Create(new Entity(EntityNames.Role)
            {
                [AttributeNames.Name] = UniqueName("role"),
                [AttributeNames.BusinessUnitId] = new EntityReference(EntityNames.BusinessUnit, BusinessUnitId)
            });
        }

        private EntityReference CreateTemplate()
        {
            return Create(new Entity(EntityNames.Template)
            {
                [AttributeNames.Title] = UniqueName("template"),
                ["templatetypecode"] = EntityNames.SystemUser,
                ["languagecode"] = 1033,
                ["ispersonal"] = false,
                [AttributeNames.Subject] = Xsl(TemplateSubject),
                [AttributeNames.Body] = Xsl("Sent by the Workflow Tools integration tests.")
            });
        }

        private static string Xsl(string text)
        {
            return "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
                + $"<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\"><![CDATA[{text}]]></xsl:template></xsl:stylesheet>";
        }

        private void CreateNote(EntityReference record, string fileName)
        {
            Create(new Entity(EntityNames.Annotation)
            {
                [AttributeNames.ObjectId] = record,
                [AttributeNames.Subject] = fileName,
                [AttributeNames.FileName] = fileName,
                [AttributeNames.MimeType] = "text/plain",
                [AttributeNames.DocumentBody] = Convert.ToBase64String(Encoding.UTF8.GetBytes($"WFT test {fileName}"))
            });
        }

        private string[] AttachmentNames(EntityReference email)
        {
            return Service.RetrieveMultiple(new QueryExpression(EntityNames.ActivityMimeAttachment)
            {
                ColumnSet = new ColumnSet(AttributeNames.FileName),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.ObjectId, ConditionOperator.Equal, email.Id) } }
            }).Entities.Select(e => e.GetAttributeValue<string>(AttributeNames.FileName)).ToArray();
        }
    }
}
