using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{
    public class Common
    {
        public ITracingService tracingService;
        public IWorkflowContext context;
        public IOrganizationServiceFactory serviceFactory;
        public IOrganizationService service;

        /// <summary>
        /// Used by the workflow activities: pulls the tracing service, workflow context and organization service from the execution context.
        /// </summary>
        public Common(CodeActivityContext executionContext)
        {
            tracingService = executionContext.GetExtension<ITracingService>();
            context = executionContext.GetExtension<IWorkflowContext>();
            serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            service = serviceFactory.CreateOrganizationService(context.UserId);
        }

        /// <summary>
        /// Used by unit tests and the console app, where no workflow execution context exists.
        /// </summary>
        public Common(IOrganizationService service, ITracingService tracingService = null, IWorkflowContext context = null)
        {
            this.service = service;
            this.tracingService = tracingService ?? new NullTracingService();
            this.context = context;
        }

        /// <summary>
        /// Writes a message to the trace log exactly as given, so it may contain { and } (interpolated
        /// values, JSON, FetchXML, stack traces).
        /// </summary>
        public void Trace(string message)
        {
            tracingService.Trace("{0}", message);
        }

        /// <summary>
        /// Writes a composite-format message (string.Format style) to the trace log.
        /// </summary>
        public void Trace(string format, params object[] args)
        {
            tracingService.Trace(format, args);
        }

        /// <summary>
        /// Tracing service that discards everything, so tracingService is never null.
        /// </summary>
        private sealed class NullTracingService : ITracingService
        {
            public void Trace(string format, params object[] args)
            {
            }
        }

        /// <summary>
        /// Query the Metadata to get the Entity Schema Name from the Object Type Code
        /// </summary>
        /// <param name="objectTypeCode"></param>
        /// <returns>Entity Schema Name</returns>
        public string GetEntityNameFromCode(string objectTypeCode)
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, Convert.ToInt32(objectTypeCode)));

            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var request = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression,
                ClientVersionStamp = null
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            var entityMetadata = response.EntityMetadata[0];

            return entityMetadata.SchemaName.ToLower();
        }

        /// <summary>
        /// Returns the primary record if it is associated with the given record through an N:N relationship.
        /// </summary>
        /// <param name="primaryEntityName">Logical name of the primary record.</param>
        /// <param name="primaryEntityId">Id of the primary record.</param>
        /// <param name="intersectEntityName">Name of the N:N intersect entity.</param>
        /// <param name="entityName">Logical name of the related record.</param>
        /// <param name="parentId">Id of the related record.</param>
        public EntityCollection GetAssociations(string primaryEntityName, Guid primaryEntityId, string intersectEntityName, string entityName, string parentId)
        {
            Trace($"Associations: {primaryEntityName} {primaryEntityId} via {intersectEntityName} to {entityName} {parentId}");

            return service.RetrieveMultiple(Queries.Associations(primaryEntityName, primaryEntityId, intersectEntityName, entityName, new Guid(parentId)));
        }

        /// <summary>
        /// Intersect entity of an N:N relationship. A name that is not a relationship is returned unchanged,
        /// so workflows configured with the intersect entity name keep working.
        /// </summary>
        public string GetIntersectEntityName(string relationshipName)
        {
            RetrieveRelationshipResponse response;

            try
            {
                response = (RetrieveRelationshipResponse)service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                Trace($"Relationship '{relationshipName}' not found; using it as the intersect entity name.");

                return relationshipName;
            }

            if (!(response.RelationshipMetadata is ManyToManyRelationshipMetadata relationship))
            {
                throw new InvalidPluginExecutionException($"Relationship '{relationshipName}' is not Many to Many.");
            }

            return relationship.IntersectEntityName;
        }

        /// <summary>
        /// Counts the records a query returns, page by page, so there is no 5,000-row or 50,000-row aggregate limit.
        /// </summary>
        public int CountRecords(QueryExpression query)
        {
            query.ColumnSet = new ColumnSet(false);
            query.PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 };

            var count = 0;

            while (true)
            {
                var page = service.RetrieveMultiple(query);
                count += page.Entities.Count;

                if (!page.MoreRecords)
                {
                    return count;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
        }

        /// <summary>
        /// Converts FetchXML to a QueryExpression (FetchXmlToQueryExpressionRequest), so it can be paged and counted.
        /// </summary>
        public QueryExpression FetchXmlToQueryExpression(string fetchXml)
        {
            var response = (FetchXmlToQueryExpressionResponse)service.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml });
            return response.Query;
        }

        /// <summary>
        /// Labels of an option set (choice) or multi-select option set attribute, by value, in the user's language.
        /// </summary>
        public Dictionary<int, string> GetOptionSetLabels(string entityName, string attributeName)
        {
            var response = (RetrieveAttributeResponse)service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = false
            });

            if (!(response.AttributeMetadata is EnumAttributeMetadata metadata))
            {
                throw new InvalidPluginExecutionException($"Attribute {attributeName} is not an option set (choice) attribute.");
            }

            return metadata.OptionSet.Options
                .Where(o => o.Value.HasValue)
                .ToDictionary(
                    o => o.Value.Value,
                    o => o.Label?.UserLocalizedLabel?.Label ?? o.Label?.LocalizedLabels.FirstOrDefault()?.Label ?? o.Value.Value.ToString());
        }

        /// <summary>
        /// Returns the ids of every record a query returns, page by page (no 5,000-row limit).
        /// </summary>
        public List<Guid> RetrieveAllIds(QueryExpression query)
        {
            query.ColumnSet = new ColumnSet(false);
            query.PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 };

            var ids = new List<Guid>();

            while (true)
            {
                var page = service.RetrieveMultiple(query);
                ids.AddRange(page.Entities.Select(e => e.Id));

                if (!page.MoreRecords)
                {
                    return ids;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
        }

        /// <summary>
        /// Starts an on-demand workflow for each record.
        /// </summary>
        public void ExecuteWorkflow(Guid workflowId, IEnumerable<Guid> recordIds)
        {
            foreach (var recordId in recordIds)
            {
                service.Execute(new ExecuteWorkflowRequest { EntityId = recordId, WorkflowId = workflowId });
            }
        }

        /// <summary>
        /// Ids of the records on the "many" side of a 1:N relationship whose lookup points at <paramref name="parentId"/>.
        /// </summary>
        public List<Guid> GetOneToManyRelatedIds(string relationshipName, Guid parentId)
        {
            var response = (RetrieveRelationshipResponse)service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });

            if (!(response.RelationshipMetadata is OneToManyRelationshipMetadata relationship))
            {
                throw new InvalidPluginExecutionException($"Relationship '{relationshipName}' is not One to Many.");
            }

            return RetrieveAllIds(Queries.ChildRecords(relationship.ReferencingEntity, relationship.ReferencingAttribute, parentId));
        }

        /// <summary>
        /// Ids of the records associated with a record through an N:N relationship. For a self-referencing
        /// relationship both directions are included and the record itself is left out.
        /// </summary>
        public List<Guid> GetManyToManyRelatedIds(string relationshipName, string primaryEntityName, Guid primaryEntityId)
        {
            var response = (RetrieveRelationshipResponse)service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });

            if (!(response.RelationshipMetadata is ManyToManyRelationshipMetadata relationship))
            {
                throw new InvalidPluginExecutionException($"Relationship '{relationshipName}' is not Many to Many.");
            }

            var intersect = relationship.IntersectEntityName;

            if (relationship.Entity1LogicalName == primaryEntityName && relationship.Entity2LogicalName == primaryEntityName)
            {
                var ids = new HashSet<Guid>(RetrieveAllIds(Queries.ManyToManyRelated(primaryEntityName, $"{primaryEntityName}id", relationship.Entity2IntersectAttribute, intersect, relationship.Entity1IntersectAttribute, primaryEntityId)));
                ids.UnionWith(RetrieveAllIds(Queries.ManyToManyRelated(primaryEntityName, $"{primaryEntityName}id", relationship.Entity1IntersectAttribute, intersect, relationship.Entity2IntersectAttribute, primaryEntityId)));
                ids.Remove(primaryEntityId);

                return ids.ToList();
            }

            if (relationship.Entity1LogicalName == primaryEntityName)
            {
                return RetrieveAllIds(Queries.ManyToManyRelated(relationship.Entity2LogicalName, relationship.Entity2IntersectAttribute, relationship.Entity2IntersectAttribute, intersect, relationship.Entity1IntersectAttribute, primaryEntityId));
            }

            return RetrieveAllIds(Queries.ManyToManyRelated(relationship.Entity1LogicalName, relationship.Entity1IntersectAttribute, relationship.Entity1IntersectAttribute, intersect, relationship.Entity2IntersectAttribute, primaryEntityId));
        }

        public List<string> GetEntityAttributesToClone(string entityName, ref string primaryIdAttribute, ref string primaryNameAttribute)
        {
            var atts = new List<string>();
            var request = new RetrieveEntityRequest()
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            };

            var response = (RetrieveEntityResponse)service.Execute(request);
            primaryIdAttribute = response.EntityMetadata.PrimaryIdAttribute;

            foreach (var attMetadata in response.EntityMetadata.Attributes)
            {
                if (attMetadata.IsPrimaryName != null && attMetadata.IsPrimaryName.Value)
                {
                    primaryNameAttribute = attMetadata.LogicalName;
                }

                if (attMetadata.IsValidForCreate != null && 
                    ((!attMetadata.IsValidForCreate.Value && 
                      !attMetadata.IsValidForUpdate.Value)
                    || attMetadata.IsPrimaryId.Value))
                {
                    continue;
                }

                atts.Add(attMetadata.AttributeTypeName.Value.ToLower() == "partylisttype"
                    ? $"partylist-{attMetadata.LogicalName}"
                    : attMetadata.LogicalName);
            }

            return atts;
        }

        public Guid CloneRecord(string entityName, string objectId, string fieldstoIgnore, string prefix)
        {
            Trace("entering CloneRecord");
            if (fieldstoIgnore == null)
            {
                fieldstoIgnore = string.Empty;
            }

            fieldstoIgnore = fieldstoIgnore.ToLower();
            Trace($"{nameof(fieldstoIgnore)}={fieldstoIgnore}");

            var retrievedObject = service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            Trace("retrieved object OK");

            var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;

            var attributesToClone = GetEntityAttributesToClone(entityName, ref primaryIdAttribute, ref primaryNameAttribute);

            foreach (var att in attributesToClone)
            {
                if (!string.IsNullOrEmpty(fieldstoIgnore))
                {
                    if (Array.IndexOf(fieldstoIgnore.Split(';'), att) >= 0 || Array.IndexOf(fieldstoIgnore.Split(','), att) >= 0)
                    {
                        continue;
                    }
                }

                if ((!retrievedObject.Attributes.Contains(att) || att == "statuscode" || att == "statecode")
                    && !att.StartsWith("partylist-"))
                {
                    continue;
                }

                EntityCollection arrPartiesNew = new EntityCollection();
                if (att.StartsWith("partylist-"))
                {
                    var att2 = att.Replace("partylist-", string.Empty);

                    var participationTypeMask = Utility.GetParticipation(att2);
                    if (string.IsNullOrEmpty(participationTypeMask))
                    {
                        throw new InvalidPluginExecutionException($"Unsupported party list attribute '{att2}'.");
                    }

                    var returnCollection = service.RetrieveMultiple(
                        Queries.ActivityParties(new Guid(objectId), int.Parse(participationTypeMask)));

                    Trace("attribute:{0}", att2);

                    foreach (var ent in returnCollection.Entities)
                    {
                        var partyid = (EntityReference)ent.Attributes["partyid"];

                        // one activityparty per party (re-using one entity threw "same key" for a second party)
                        var party = new Entity("activityparty");
                        party.Attributes.Add("partyid", new EntityReference(partyid.LogicalName, partyid.Id));
                        Trace("attribute:{0}:{1}:{2}", att2, partyid.LogicalName, partyid.Id.ToString());
                        arrPartiesNew.Entities.Add(party);
                    }

                    newEntity.Attributes.Add(att2, arrPartiesNew);
                    continue;
                }

                Trace("attribute:{0}", att);

                if (att == primaryNameAttribute && prefix != null)
                {
                    retrievedObject.Attributes[att] = prefix + retrievedObject.Attributes[att];
                }

                newEntity.Attributes.Add(att, retrievedObject.Attributes[att]);
            }

            Trace("creating cloned object...");
            var id = service.Create(newEntity);
            Trace("created cloned object OK");

            if (newEntity.Attributes.Contains("statuscode") && newEntity.Attributes.Contains("statecode"))
            {
                var record = service.Retrieve(entityName, id, new ColumnSet("statuscode", "statecode"));

                if (retrievedObject.Attributes["statuscode"] != record.Attributes["statuscode"] ||
                    retrievedObject.Attributes["statecode"] != record.Attributes["statecode"])
                {
                    var setStatusEnt = new Entity(entityName, id);
                    setStatusEnt.Attributes.Add("statuscode", retrievedObject.Attributes["statuscode"]);
                    setStatusEnt.Attributes.Add("statecode", retrievedObject.Attributes["statecode"]);

                    service.Update(setStatusEnt);
                }
            }

            Trace("cloned object OK");

            return id;
        }

        public void DeleteRecordAuditHistory(string logicalName, string id)
        {
            var delRequest = new DeleteRecordChangeHistoryRequest();

            var objt = new EntityReference(logicalName, new Guid(id));

            delRequest.Target = objt;
            service.Execute(delRequest);
        }

        public EntityReference retrieveUserBUDefaultTeam(string systemuserid)
        {
            var teamres = new EntityReference("team");

            var team = service.RetrieveMultiple(Queries.DefaultTeamForUser(new Guid(systemuserid)));

            teamres.Id = team.Entities[0].Id;
            return teamres;
        }
        /*
        public void QRCode(string entityname, string recordid, string QRInfo, string noteSubject, string noteText, string fileName)
        {
            Trace("1");
            QRCodeEncoder encoder = new QRCodeEncoder();
            Trace("2");
            Bitmap hi = encoder.Encode(QRInfo);
            Trace("3");
            string base64String = String.Empty;
            Trace("4");
            using (MemoryStream ms = new MemoryStream())
            {
                Trace("read stream");
                hi.Save(ms, ImageFormat.Jpeg);
            
            
                byte[] imageBytes = ms.ToArray();
                base64String = Convert.ToBase64String(imageBytes);
            }
            Entity Annotation = new Entity("annotation");
            Annotation.Attributes["objectid"] = new EntityReference(entityname, new Guid(recordid));
            Annotation.Attributes["objecttypecode"] = entityname;
            Annotation.Attributes["subject"] = noteSubject;
            Annotation.Attributes["documentbody"] = base64String;
            Annotation.Attributes["mimetype"] = @"image/jpeg";
            Annotation.Attributes["notetext"] = noteText;
            Annotation.Attributes["filename"] = fileName;
            service.Create(Annotation);
            /*

            ------------


             QRCodeGenerator qrGenerator = new QRCodeGenerator();
             QRCodeData qrCodeData = qrGenerator.CreateQrCode(QRInfo, QRCodeGenerator.ECCLevel.Q);
             QRCode qrCode = new QRCode(qrCodeData);
             Bitmap qrCodeImage = qrCode.GetGraphic(20);
             string base64String = String.Empty;
             using (MemoryStream ms = new MemoryStream())
             {
                 switch (imageFormat)
                 {
                     case "jpg":
                     case "jpeg":
                         qrCodeImage.Save(ms, ImageFormat.Jpeg);
                         break;
                     case "bmp":
                         qrCodeImage.Save(ms, ImageFormat.Bmp);
                         break;
                     case "gif":
                         qrCodeImage.Save(ms, ImageFormat.Gif);
                         break;
                     case "png":
                         qrCodeImage.Save(ms, ImageFormat.Png);
                         break;

                 }
                 qrCodeImage.Save(ms, ImageFormat.Jpeg);
                 byte[] imageBytes = ms.ToArray();
                 base64String = Convert.ToBase64String(imageBytes);
             }
             if (noteSubject == "")
             {
                 noteSubject = "QR";
             }
             Entity Annotation = new Entity("annotation");
             Annotation.Attributes["objectid"] = new EntityReference(entityname,new Guid(recordid));
             Annotation.Attributes["objecttypecode"] = entityname;
             Annotation.Attributes["subject"] = noteSubject;
             Annotation.Attributes["documentbody"] = base64String;
             Annotation.Attributes["mimetype"] = @"image/jpeg";
             Annotation.Attributes["notetext"] = noteText;
             Annotation.Attributes["filename"] =fileName;
             service.Create(Annotation);
             
    *

        }*/

        public bool SendEmailToUsersInRole(EntityReference securityRoleLookup, EntityReference email)
        {
            var userList = service.RetrieveMultiple(Queries.UsersInRole(securityRoleLookup.Id));
            Trace("Retrieved Data");

            var emailEnt = new Entity("email", email.Id);

            var to = new EntityCollection();

            foreach (var user in userList.Entities)
            {
                // Id of the user
                var userId = user.Id;

                var to1 = new Entity("activityparty");
                to1["partyid"] = new EntityReference("systemuser", userId);

                to.Entities.Add(to1);
            }
            emailEnt["to"] = to;

            service.Update(emailEnt);

            var req = new SendEmailRequest();
            req.EmailId = email.Id;

            var res = (SendEmailResponse)service.Execute(req);
            return true;
        }

        public bool SendEmailFromTemplateToUsersInRole(EntityReference securityRoleLookup, EntityReference emailTemplateLookup)
        {
            var userList = service.RetrieveMultiple(Queries.UsersInRole(securityRoleLookup.Id));
            Trace("Retrieved Data");

            foreach (var user in userList.Entities)
            {
                try
                {
                    Trace("user creating email");
                    var sent = SendEmailFromTemplate(emailTemplateLookup, user.Id);
                }
                catch (System.Exception ex)
                {
                    Trace($"error:{ex.ToString()}");
                }
            }
            return true;
        }

        public bool SendEmailFromTemplate(EntityReference template, Guid userId)
        {
            var toEntities = new List<Entity>();
            var activityParty = new Entity();
            activityParty.LogicalName = "activityparty";
            activityParty.Attributes["partyid"] = new EntityReference("systemuser", userId);
            toEntities.Add(activityParty);

            var email = new Entity("email");
            email.Attributes["to"] = toEntities.ToArray();

            var emailUsingTemplateReq = new SendEmailFromTemplateRequest
            {
                Target = email,

                // Use a built-in Email Template of type "contact".
                TemplateId = template.Id,

                // The regarding Id is required, and must be of the same type as the Email Template.
                RegardingId = userId,
                RegardingType = "systemuser"
            };

            var emailUsingTemplateResp = (SendEmailFromTemplateResponse)service.Execute(emailUsingTemplateReq);

            return true;
        }

        public string GetAppModuleId(string appModuleUniqueName)
        {
            var query = new QueryExpression
            {
                EntityName = "appmodule",
                ColumnSet = new ColumnSet("appmoduleid", "uniquename"),
                Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression ("uniquename", ConditionOperator.Equal, appModuleUniqueName)
                            }
                        }
            };

            var appmodules = service.RetrieveMultiple(query).Entities;
            return appmodules.First()["appmoduleid"].ToString();
        }

        public string GetAppRecordUrl(string recordUrl, string appModuleUniqueName)
        {
            var appModuleId = GetAppModuleId(appModuleUniqueName);

            return $"{recordUrl}&appid={appModuleId}";
        }

        /// <summary>
        /// Finds the copy of a security role that belongs to a team's or user's business unit.
        /// Roles are copied into every business unit and a principal can only hold the copy from its own
        /// business unit, so this reads the role's root role and returns the role with that root in the
        /// principal's business unit.
        /// </summary>
        /// <param name="principal">The team or systemuser whose business unit is used.</param>
        /// <param name="roleId">Any copy of the role (usually the one picked in the workflow).</param>
        /// <returns>The role id in the principal's business unit, or null if <paramref name="roleId"/> does not exist.</returns>
        public Guid? GetRoleIdInBusinessUnit(EntityReference principal, Guid roleId)
        {
            var principalRecord = service.Retrieve(principal.LogicalName, principal.Id, new ColumnSet("businessunitid"));
            var businessUnit = (EntityReference)principalRecord.Attributes["businessunitid"];

            var roleQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("parentrootroleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "roleid",
                            Operator = ConditionOperator.Equal,
                            Values = { roleId }
                        }
                    }
                }
            };

            var givenRoles = service.RetrieveMultiple(roleQuery);

            if (givenRoles.Entities.Count <= 0)
            {
                return null;
            }

            var givenRole = givenRoles.Entities[0];
            var rootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

            Trace("Role {0} is retrieved.", givenRole.Id);

            var businessUnitRoleQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("roleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "parentrootroleid",
                            Operator = ConditionOperator.Equal,
                            Values = { rootRole.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = "businessunitid",
                            Operator = ConditionOperator.Equal,
                            Values = { businessUnit.Id }
                        }
                    }
                }
            };

            var businessUnitRoles = service.RetrieveMultiple(businessUnitRoleQuery);

            return (Guid)businessUnitRoles.Entities[0].Attributes["roleid"];
        }

        public bool IsMemberOfTeam(Guid teamId, Guid userId)
        {
            var query = new QueryExpression
            {
                EntityName = "teammembership",
                ColumnSet = new ColumnSet("systemuserid", "teamid"),
                Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression ("systemuserid", ConditionOperator.Equal, userId),
                                new ConditionExpression ("teamid", ConditionOperator.Equal, teamId)
                            }
                        }
            };

            //get the results
            var retrievedUsers = service.RetrieveMultiple(query);

            return retrievedUsers.Entities.Count > 0;
        }

        /// <summary>
        /// Finds the copy of a security role that belongs to a team's or user's business unit.
        /// Roles are copied into every business unit and a principal can only hold the copy from its own
        /// business unit, so this reads the role's root role and returns the role with that root in the
        /// principal's business unit.
        /// </summary>
        /// <param name="principal">The team or systemuser whose business unit is used.</param>
        /// <param name="roleId">Any copy of the role (usually the one picked in the workflow).</param>
        /// <returns>The role id in the principal's business unit, or null if <paramref name="roleId"/> does not exist.</returns>
        public Guid? GetRoleIdInBusinessUnit(EntityReference principal, Guid roleId)
        {
            var principalRecord = service.Retrieve(principal.LogicalName, principal.Id, new ColumnSet("businessunitid"));
            var businessUnit = (EntityReference)principalRecord.Attributes["businessunitid"];

            var roleQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("parentrootroleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "roleid",
                            Operator = ConditionOperator.Equal,
                            Values = { roleId }
                        }
                    }
                }
            };

            var givenRoles = service.RetrieveMultiple(roleQuery);

            if (givenRoles.Entities.Count <= 0)
            {
                return null;
            }

            var givenRole = givenRoles.Entities[0];
            var rootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

            Trace("Role {0} is retrieved.", givenRole.Id);

            var businessUnitRoleQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("roleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "parentrootroleid",
                            Operator = ConditionOperator.Equal,
                            Values = { rootRole.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = "businessunitid",
                            Operator = ConditionOperator.Equal,
                            Values = { businessUnit.Id }
                        }
                    }
                }
            };

            var businessUnitRoles = service.RetrieveMultiple(businessUnitRoleQuery);

            return (Guid)businessUnitRoles.Entities[0].Attributes["roleid"];
        }

        /// <summary>
        /// The record a record URL points at, with the entity name looked up from the URL's type code.
        /// </summary>
        public EntityReference GetRecordReference(string recordUrl)
        {
            var parsedUrl = Utility.ParseRecordUrl(recordUrl);

            Trace($"ObjectTypeCode={parsedUrl.ObjectTypeCode}--ParentId={parsedUrl.Id}");

            return new EntityReference(GetEntityNameFromCode(parsedUrl.ObjectTypeCode), new Guid(parsedUrl.Id));
        }

        /// <summary>
        /// Grants a user or team access to the record a record URL points at. A null principal grants nothing.
        /// </summary>
        public void ShareRecord(string recordUrl, EntityReference principal, AccessRights accessMask)
        {
            var target = GetRecordReference(recordUrl);

            Trace("Grant Request--- Start");

            if (principal != null)
            {
                service.Execute(new GrantAccessRequest
                {
                    Target = target,
                    PrincipalAccess = new PrincipalAccess { Principal = principal, AccessMask = accessMask }
                });
            }

            Trace("Grant Request--- end");
        }

        /// <summary>
        /// Removes a user's or team's shared access to the record a record URL points at. A null principal revokes nothing.
        /// </summary>
        public void UnshareRecord(string recordUrl, EntityReference principal)
        {
            var target = GetRecordReference(recordUrl);

            if (principal != null)
            {
                service.Execute(new RevokeAccessRequest { Target = target, Revokee = principal });
            }

            Trace("Revoked Permissions--- OK");
        }

        public void DeleteOptionValue(bool globalOptionSet, string attributeName, string entityName, int optionValue)
        {
            if (globalOptionSet)
            {
                var deleteOptionValueRequest =
                  new DeleteOptionValueRequest
                  {
                      OptionSetName = attributeName,
                      Value = optionValue
                  };
                service.Execute(deleteOptionValueRequest);
            }
            else
            {
                // Create a request.
                var insertOptionValueRequest =
                   new DeleteOptionValueRequest
                   {
                       AttributeLogicalName = attributeName,
                       EntityLogicalName = entityName,
                       Value = optionValue
                   };
                service.Execute(insertOptionValueRequest);
            }
        }

        public void SalesLiteratureToEmail(string _FileName, string salesLiteratureId, string emailid)
        {
            if (_FileName == "*")
            {
                _FileName = string.Empty;
            }
            _FileName = _FileName.Replace("*", "%");

            #region "Query Attachments"
            var fileNamePattern = $"%{_FileName}%";
            Trace($"Sales literature items: file name like '{fileNamePattern}', sales literature {salesLiteratureId}");
            var attachmentFiles = service.RetrieveMultiple(Queries.SalesLiteratureItems(fileNamePattern, new Guid(salesLiteratureId)));

            if (attachmentFiles.Entities.Count == 0)
            {
                Trace("No Attachment Files found.");
                return;
            }

            #endregion

            #region "Add Attachments to Email"
            var i = 1;
            foreach (var file in attachmentFiles.Entities)
            {
                var _Attachment = new Entity("activitymimeattachment");
                _Attachment["objectid"] = new EntityReference("email", new Guid(emailid));
                _Attachment["objecttypecode"] = "email";
                _Attachment["attachmentnumber"] = i;
                i++;

                if (file.Attributes.Contains("title"))
                {
                    _Attachment["subject"] = file.Attributes["title"].ToString();
                }
                if (file.Attributes.Contains("filename"))
                {
                    _Attachment["filename"] = file.Attributes["filename"].ToString();
                }
                if (file.Attributes.Contains("documentbody"))
                {
                    _Attachment["body"] = file.Attributes["documentbody"].ToString();
                }
                if (file.Attributes.Contains("mimetype"))
                {
                    _Attachment["mimetype"] = file.Attributes["mimetype"].ToString();
                }

                service.Create(_Attachment);
            }

            #endregion
        }

        public bool InsertOptionValue(bool globalOptionSet, string attributeName, string entityName, string optionText, int optionValue, int languageCode)
        {
            if (globalOptionSet)
            {
                var insertOptionValueRequest =
                  new InsertOptionValueRequest
                  {
                      OptionSetName = attributeName,
                      Value = optionValue,
                      Label = new Label(optionText, languageCode)
                  };
                var insertOptionValue = ((InsertOptionValueResponse)service.Execute(insertOptionValueRequest)).NewOptionValue;
            }
            else
            {
                // Create a request.
                var insertOptionValueRequest =
                   new InsertOptionValueRequest
                   {
                       AttributeLogicalName = attributeName,
                       EntityLogicalName = entityName,
                       Value = optionValue,
                       Label = new Label(optionText, languageCode)
                   };
                var insertOptionValue = ((InsertOptionValueResponse)service.Execute(insertOptionValueRequest)).NewOptionValue;
            }
            return true;
        }

        public Guid CreateTeam(string teamName, int teamType, EntityReference administrator, EntityReference businessUnit)
        {
            var team = new Entity("team");
            team.Attributes.Add("administratorid", administrator);
            team.Attributes.Add("name", teamName);
            team.Attributes.Add("teamtype", new OptionSetValue(teamType));
            team.Attributes.Add("businessunitid",  businessUnit);

            var _teamId = service.Create(team);

            return _teamId;
        }
        public void AssociateEntity(string PrimaryEntityName, Guid PrimaryEntityId, string _relationshipName, string _relationshipEntityName, string entityName, string ParentId)
        {
            try
            {
                var relations = GetAssociations(PrimaryEntityName, PrimaryEntityId, _relationshipEntityName, entityName, ParentId);

                if (relations.Entities.Count == 0)
                {
                    var relatedEntities = new EntityReferenceCollection();
                    relatedEntities.Add(new EntityReference(entityName, new Guid(ParentId)));
                    var relationship = new Relationship(_relationshipName);
                    if (PrimaryEntityName == entityName)
                    {
                        relationship.PrimaryEntityRole = EntityRole.Referencing;
                    }

                    service.Associate(PrimaryEntityName, PrimaryEntityId, relationship, relatedEntities);
                }
            }
            catch (Exception ex)
            {
                Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //    common.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);//
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
        }

        public void EntityAttachmentToEmail(string fileName, string parentId, EntityReference email, bool retrieveActivityMimeAttachment, bool mostRecent, int? topRecords = 0)
        {
            #region "Query Attachments"

            Trace($"Attachments: {(retrieveActivityMimeAttachment ? "activitymimeattachment" : "annotation")} of {parentId}, file name like '{fileName}', top {topRecords}");
            var attachmentFiles = service.RetrieveMultiple(
                Queries.EntityAttachments(retrieveActivityMimeAttachment, fileName, new Guid(parentId), topRecords ?? 0));

            if (attachmentFiles.Entities.Count == 0)
            {
                Trace("No Attachment Files found.");
                return;
            }

            #endregion

            #region "Add Attachments to Email"

            var i = 1;
            var attachedFiles = new List<Entity>();

            foreach (var file in attachmentFiles.Entities)
            {
                Trace("Entities Count: {0} ", i);

                var _Attachment = new Entity("activitymimeattachment");
                _Attachment["objectid"] = new EntityReference("email", email.Id);
                _Attachment["objecttypecode"] = "email";
                _Attachment["attachmentnumber"] = i;
                i++;

                if (file.Attributes.Contains("subject"))
                {
                    _Attachment["subject"] = file.Attributes["subject"].ToString();
                }
                if (file.Attributes.Contains("filename"))
                {
                    _Attachment["filename"] = file.Attributes["filename"].ToString();
                }
                if (file.Attributes.Contains("documentbody"))
                {
                    _Attachment["body"] = file.Attributes["documentbody"].ToString();
                }
                else if (file.Attributes.Contains("body"))
                {
                    _Attachment["body"] = file.Attributes["body"].ToString();
                }
                if (file.Attributes.Contains("mimetype"))
                {
                    _Attachment["mimetype"] = file.Attributes["mimetype"].ToString();
                }

                if (mostRecent)
                {
                    Trace("Is Most Recent");

                    var alreadyAttached = attachedFiles.Where(f => f["filename"].ToString() == file.GetAttributeValue<string>("filename")).FirstOrDefault();

                    if (alreadyAttached == null)
                    {
                        Trace("not already attached");

                        service.Create(_Attachment);

                        if (!file.Contains("filename"))
                        {
                            file["filename"] = string.Empty;
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
                    service.Create(_Attachment);
                }
            }

            #endregion
        }

        /// <summary>
        /// Retrieves related records using a relationship metadata
        /// </summary>
        /// <param name="relationshipName">relationship to navigate</param>
        /// <param name="parentEntityId">Parent Id</param>
        /// <returns></returns>
        public EntityCollection GetChildRecords(string relationshipName, string parentEntityId)
        {
            //1) Get child lookup field name
            var req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };
            var res = (RetrieveRelationshipResponse)service.Execute(req);
            var rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            var childEntityType = rel.ReferencingEntity;
            var childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            var querybyattribute = new QueryByAttribute(childEntityType);
            querybyattribute.ColumnSet = new ColumnSet(childEntityFieldName);
            querybyattribute.Attributes.AddRange(childEntityFieldName);
            querybyattribute.Values.AddRange(new Guid(parentEntityId));
            var retrieved = service.RetrieveMultiple(querybyattribute);

            return retrieved;
        }

        public void UpdateChildRecords(string relationshipName, string parentEntityType, string parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate, bool _UpdateonlyActive)
        {
            //1) Get child lookup field name
            var req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };
            var res = (RetrieveRelationshipResponse)service.Execute(req);
            var rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            var childEntityType = rel.ReferencingEntity;
            var childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            var querybyattribute = new QueryByAttribute(childEntityType);
            querybyattribute.ColumnSet = new ColumnSet(childEntityFieldName);

            if (!_UpdateonlyActive)
            {
                querybyattribute.Attributes.AddRange(childEntityFieldName);
                querybyattribute.Values.AddRange(new Guid(parentEntityId));
            }
            else
            {
                querybyattribute.Attributes.AddRange(childEntityFieldName, "statecode");
                querybyattribute.Values.AddRange(new Guid(parentEntityId), 0);
            }
            var retrieved = service.RetrieveMultiple(querybyattribute);

            //2') retrieve parent fielv value
            var valueToUpdate = new object();
            if (!string.IsNullOrEmpty(parentFieldNameToUpdate))
            {
                var retrievedEntity = (Entity)service.Retrieve(parentEntityType, new Guid(parentEntityId), new ColumnSet(parentFieldNameToUpdate));
                if (retrievedEntity.Attributes.Contains(parentFieldNameToUpdate))
                {
                    valueToUpdate = retrievedEntity.Attributes[parentFieldNameToUpdate];
                }
                else
                {
                    valueToUpdate = null;
                }
            }
            else
            {
                valueToUpdate = setValueToUpdate;
            }

            //3) update each child record

            foreach (var child in retrieved.Entities)
            {
                if (childEntityType.ToLower() == "dynamicpropertyinstance")
                {
                    //pending...
                    var req2 = new UpdateProductPropertiesRequest();
                    // req2.
                    break;
                }

                var reqAtt = new RetrieveAttributeRequest();
                reqAtt.EntityLogicalName = childEntityType;
                reqAtt.LogicalName = childFieldNameToUpdate;
                var resAtt = (RetrieveAttributeResponse)service.Execute(reqAtt);

                //var valueToUpdateBool = false;
                var meta = resAtt.AttributeMetadata;

                var entUpdate = new Entity(childEntityType)
                {
                    Id = child.Id
                };

                if (meta.AttributeType.Value.ToString() == "Boolean")
                {
                    // valueToUpdate is an object, so compare its text (== "1" compared references and missed "1" read from a field)
                    var text = Convert.ToString(valueToUpdate, CultureInfo.InvariantCulture);
                    var isTrue = valueToUpdate is bool flag
                        ? flag
                        : text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);

                    entUpdate.Attributes.Add(childFieldNameToUpdate, isTrue);
                }
                else
                {
                    if (meta.AttributeType.Value.ToString() == "Picklist" || meta.AttributeType.Value.ToString() == "Status")
                    {
                        if (valueToUpdate == null)
                        {
                            entUpdate.Attributes.Add(childFieldNameToUpdate, null);
                        }
                        else
                        {
                            if (valueToUpdate is OptionSetValue)
                            {
                                valueToUpdate = ((OptionSetValue)valueToUpdate).Value;
                            }
                            var opt = new OptionSetValue(Convert.ToInt32(valueToUpdate));
                            entUpdate.Attributes.Add(childFieldNameToUpdate, opt);
                        }
                    }
                    else
                    {
                        //if (meta.AttributeType.Value.ToString() == "EntityReference")
                        //{
                        //    EntityReference valueRef = (EntityReference)valueToUpdate;
                        //    EntityReference entR = new EntityReference(valueRef.LogicalName,new Guid(valueRef.Id.ToString()));
                        //    entUpdate.Attributes.Add(childFieldNameToUpdate, entR);
                        //}
                        //else
                        {
                            entUpdate.Attributes.Add(childFieldNameToUpdate, valueToUpdate);
                        }
                    }
                }

                service.Update(entUpdate);
            }
        }
    }
}