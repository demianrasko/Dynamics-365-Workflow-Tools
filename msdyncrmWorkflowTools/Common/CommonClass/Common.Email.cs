using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        public void SendEmailFromTemplate(EntityReference template, Guid userId)
        {
            var toEntities = new List<Entity>();
            var activityParty = new Entity
            {
                LogicalName = EntityNames.ActivityParty,
                Attributes =
                {
                    [AttributeNames.PartyId] = new EntityReference(EntityNames.SystemUser, userId)
                }
            };

            toEntities.Add(activityParty);

            var email = new Entity(EntityNames.Email)
            {
                Attributes =
                {
                    [AttributeNames.To] = toEntities.ToArray()
                }
            };

            var request = new SendEmailFromTemplateRequest
            {
                Target = email,

                // Use a built-in Email Template of type "contact".
                TemplateId = template.Id,

                // The regarding Id is required, and must be of the same type as the Email Template.
                RegardingId = userId,
                RegardingType = EntityNames.SystemUser
            };

            Service.Execute(request);
        }

        public bool SendEmailFromTemplateToUsersInRole(EntityReference securityRoleLookup, EntityReference emailTemplateLookup)
        {
            var userList = Service.RetrieveMultiple(Queries.UsersInRole(securityRoleLookup.Id));
            Trace("Retrieved Data");

            // keep sending to the remaining users when one fails; failures are traced, not thrown
            var failures = new List<string>();

            foreach (var user in userList.Entities)
            {
                try
                {
                    Trace($"Sending template email to user {user.Id}");
                    SendEmailFromTemplate(emailTemplateLookup, user.Id);
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    Trace("{0}", Utility.HandleExceptions(ex));
                    failures.Add($"{user.Id}: {ex.Detail.Message}");
                }
            }

            if (failures.Count > 0)
            {
                Trace($"The email could not be sent to {failures.Count} of {userList.Entities.Count} users. {string.Join("; ", failures)}");
            }

            return true;
        }

        /// <summary>
        /// Sends an email (SendEmail, IssueSend).
        /// </summary>
        /// <returns>The subject of the sent email.</returns>
        public string SendEmail(Guid emailId)
        {
            var response = (SendEmailResponse)Service.Execute(new SendEmailRequest { EmailId = emailId, IssueSend = true });

            return response.Subject;
        }

        /// <summary>
        /// Sets the To recipients of an email to the given users (replacing any recipients it had).
        /// </summary>
        public void SetEmailRecipients(Guid emailId, IEnumerable<Guid> userIds)
        {
            var to = new EntityCollection();

            foreach (var userId in userIds)
            {
                to.Entities.Add(new Entity(EntityNames.ActivityParty)
                {
                    [AttributeNames.PartyId] = new EntityReference(EntityNames.SystemUser, userId)
                });
            }

            Trace($"Email {emailId}: {to.Entities.Count} recipient(s)");

            Service.Update(new Entity(EntityNames.Email, emailId)
            {
                [AttributeNames.To] = to
            });
        }

        /// <summary>
        /// Addresses an email to every member of a team. Leaves the email unchanged when the team has no members.
        /// </summary>
        /// <returns>The number of members the email was addressed to.</returns>
        public int AddressEmailToTeam(Guid emailId, Guid teamId)
        {
            var members = Service.RetrieveMultiple(Queries.TeamMembers(teamId)).Entities.Select(e => e.Id).ToList();

            if (members.Count == 0)
            {
                Trace($"Team {teamId} has no members.");
                return 0;
            }

            SetEmailRecipients(emailId, members);

            return members.Count;
        }

        public void SendEmailToUsersInRole(EntityReference securityRoleLookup, EntityReference emailReference)
        {
            var userIds = Service.RetrieveMultiple(Queries.UsersInRole(securityRoleLookup.Id)).Entities.Select(e => e.Id);

            SetEmailRecipients(emailReference.Id, userIds);

            Service.Execute(new SendEmailRequest
            {
                EmailId = emailReference.Id
            });
        }

        public void EntityAttachmentToEmail(string fileName, Guid parentId, EntityReference email, bool retrieveActivityMimeAttachment, bool mostRecent, int? topRecords = 0)
        {
            Trace($"Attachments: {(retrieveActivityMimeAttachment ? EntityNames.ActivityMimeAttachment : EntityNames.Annotation)} of {parentId}, file name like '{fileName}', top {topRecords}");
            var attachmentFiles = Service.RetrieveMultiple(
                Queries.EntityAttachments(retrieveActivityMimeAttachment, fileName, parentId, topRecords ?? 0));

            if (attachmentFiles.Entities.Count == 0)
            {
                Trace("No Attachment Files found.");
                return;
            }

            var i = 1;
            var attachedFiles = new List<Entity>();

            foreach (var file in attachmentFiles.Entities)
            {
                Trace("Entities Count: {0} ", i);

                var attachment = new Entity(EntityNames.ActivityMimeAttachment)
                {
                    [AttributeNames.ObjectId] = new EntityReference(EntityNames.Email, email.Id),
                    [AttributeNames.ObjectTypeCode] = EntityNames.Email,
                    [AttributeNames.AttachmentNumber] = i
                };
                i++;

                Utility.CopyAttributeValue(file, AttributeNames.Subject, attachment);
                Utility.CopyAttributeValue(file, AttributeNames.FileName, attachment);
                Utility.CopyAttributeValue(file, AttributeNames.MimeType, attachment);

                if (!Utility.CopyAttributeValue(file, AttributeNames.DocumentBody, attachment, AttributeNames.Body))
                {
                    Utility.CopyAttributeValue(file, AttributeNames.Body, attachment);
                }

                if (mostRecent)
                {
                    Trace("Is Most Recent");

                    var alreadyAttached = attachedFiles.Where(f => f[AttributeNames.FileName].ToString() == file.GetAttributeValue<string>(AttributeNames.FileName)).FirstOrDefault();

                    if (alreadyAttached == null)
                    {
                        Trace("not already attached");

                        Service.Create(attachment);

                        if (!file.Contains(AttributeNames.FileName))
                        {
                            file[AttributeNames.FileName] = string.Empty;
                        }

                        attachedFiles.Add(file);
                    }
                    else
                    {
                        Trace("already attached");
                    }
                }
                else
                {
                    Trace("Is Not Most Recent");
                    Service.Create(attachment);
                }
            }
        }

        public void SalesLiteratureToEmail(string fileName, Guid salesLiteratureId, Guid emailId)
        {
            if (fileName == "*")
            {
                fileName = string.Empty;
            }

            fileName = fileName.Replace("*", "%");

            var fileNamePattern = $"%{fileName}%";
            Trace($"Sales literature items: file name like '{fileNamePattern}', sales literature {salesLiteratureId}");

            var attachmentFiles = Service.RetrieveMultiple(Queries.SalesLiteratureItems(fileNamePattern, salesLiteratureId));

            if (attachmentFiles.Entities.Count == 0)
            {
                Trace("No Attachment Files found.");
                return;
            }

            var i = 1;

            foreach (var file in attachmentFiles.Entities)
            {
                var attachment = new Entity(EntityNames.ActivityMimeAttachment)
                {
                    [AttributeNames.ObjectId] = new EntityReference(EntityNames.Email, emailId),
                    [AttributeNames.ObjectTypeCode] = EntityNames.Email,
                    [AttributeNames.AttachmentNumber] = i
                };

                i++;

                Utility.CopyAttributeValue(file, AttributeNames.Title, attachment, AttributeNames.Subject);
                Utility.CopyAttributeValue(file, AttributeNames.FileName, attachment);
                Utility.CopyAttributeValue(file, AttributeNames.DocumentBody, attachment, AttributeNames.Body);
                Utility.CopyAttributeValue(file, AttributeNames.MimeType, attachment);

                Service.Create(attachment);
            }
        }
    }
}
