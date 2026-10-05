using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
            var userList = Service.RetrieveMultiple(UsersInRoleQuery(securityRoleLookup.Id));
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
                    Trace(Utility.HandleExceptions(ex));
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
            var members = Service.RetrieveMultiple(TeamMembersQuery(teamId)).Entities.Select(e => e.Id).ToList();

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
            var userIds = Service.RetrieveMultiple(UsersInRoleQuery(securityRoleLookup.Id)).Entities.Select(e => e.Id);

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
                EntityAttachmentsQuery(retrieveActivityMimeAttachment, fileName, parentId, topRecords ?? 0));

            if (attachmentFiles.Entities.Count == 0)
            {
                Trace("No Attachment Files found.");
                return;
            }

            var i = 1;
            var attachedFiles = new List<Entity>();

            foreach (var file in attachmentFiles.Entities)
            {
                Trace($"Entities Count: {i}");

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

                    var alreadyAttached = attachedFiles.FirstOrDefault(f => f[AttributeNames.FileName].ToString() == file.GetAttributeValue<string>(AttributeNames.FileName));

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

            var attachmentFiles = Service.RetrieveMultiple(SalesLiteratureItemsQuery(fileNamePattern, salesLiteratureId));

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

        /// <summary>
        /// File attachments of a record: notes with a document (newest first), or the attachments of an email/activity.
        /// </summary>
        /// <param name="activityMimeAttachments">True for activitymimeattachment (email attachments), false for annotation (notes).</param>
        /// <param name="fileNamePattern">Optional LIKE pattern for the file name, or several separated by ";"
        /// (e.g. "%.pdf;%.docx"), any of which may match; null or empty means any file.</param>
        /// <param name="parentId">The record the notes belong to, or the activity the attachments belong to.</param>
        /// <param name="top">Maximum number of records; 0 or less means no limit.</param>
        public static QueryExpression EntityAttachmentsQuery(bool activityMimeAttachments, string fileNamePattern, Guid parentId, int top)
        {
            QueryExpression query;

            if (activityMimeAttachments)
            {
                query = new QueryExpression(EntityNames.ActivityMimeAttachment)
                {
                    ColumnSet = new ColumnSet(AttributeNames.FileName, AttributeNames.AttachmentId, AttributeNames.Subject, AttributeNames.Body, AttributeNames.MimeType)
                };
                query.Criteria.AddCondition(AttributeNames.ActivityId, ConditionOperator.Equal, parentId);
            }
            else
            {
                query = new QueryExpression(EntityNames.Annotation)
                {
                    ColumnSet = new ColumnSet(AttributeNames.FileName, AttributeNames.AnnotationId, AttributeNames.Subject, AttributeNames.DocumentBody, AttributeNames.MimeType)
                };
                query.AddOrder(AttributeNames.CreatedOn, OrderType.Descending);
                query.Criteria.AddCondition(AttributeNames.IsDocument, ConditionOperator.Equal, true);
                query.Criteria.AddCondition(AttributeNames.ObjectId, ConditionOperator.Equal, parentId);
            }

            var patterns = (fileNamePattern ?? string.Empty)
                .Split(';')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (patterns.Count == 1)
            {
                query.Criteria.AddCondition(AttributeNames.FileName, ConditionOperator.Like, patterns[0]);
            }
            else if (patterns.Count > 1)
            {
                var anyPattern = query.Criteria.AddFilter(LogicalOperator.Or);

                foreach (var pattern in patterns)
                {
                    anyPattern.AddCondition(AttributeNames.FileName, ConditionOperator.Like, pattern);
                }
            }

            if (top > 0)
            {
                query.TopCount = top;
            }

            return query;
        }

        /// <summary>Sales literature items whose file name matches a LIKE pattern (% and _ wildcards).</summary>
        public static QueryExpression SalesLiteratureItemsQuery(string fileNamePattern, Guid salesLiteratureId)
        {
            var query = new QueryExpression(EntityNames.SalesLiteratureItem)
            {
                ColumnSet = new ColumnSet(AttributeNames.FileName, AttributeNames.SalesLiteratureItemId, AttributeNames.Title, AttributeNames.DocumentBody, AttributeNames.MimeType)
            };
            query.Criteria.AddCondition(AttributeNames.FileName, ConditionOperator.Like, fileNamePattern);
            query.Criteria.AddCondition(AttributeNames.SalesLiteratureId, ConditionOperator.Equal, salesLiteratureId);

            return query;
        }

        /// <summary>The users who are members of a team.</summary>
        public static QueryExpression TeamMembersQuery(Guid teamId)
        {
            var query = new QueryExpression(EntityNames.SystemUser)
            {
                ColumnSet = new ColumnSet(AttributeNames.SystemUserId)
            };

            var membership = query.AddLink(EntityNames.TeamMembership, AttributeNames.SystemUserId, AttributeNames.SystemUserId);
            membership.LinkCriteria.AddCondition(AttributeNames.TeamId, ConditionOperator.Equal, teamId);

            return query;
        }

        /// <summary>Enabled, read-write users that hold a security role.</summary>
        public static QueryExpression UsersInRoleQuery(Guid roleId)
        {
            var query = new QueryExpression(EntityNames.SystemUser)
            {
                ColumnSet = new ColumnSet(AttributeNames.SystemUserId),
                Distinct = true
            };
            query.Criteria.AddCondition(AttributeNames.AccessMode, ConditionOperator.Equal, 0);

            var userRoles = query.AddLink(EntityNames.SystemUserRoles, AttributeNames.SystemUserId, AttributeNames.SystemUserId);
            var role = userRoles.AddLink(EntityNames.Role, AttributeNames.RoleId, AttributeNames.RoleId);
            role.EntityAlias = "aa";
            role.LinkCriteria.AddCondition(AttributeNames.RoleId, ConditionOperator.Equal, roleId);

            return query;
        }
    }
}
