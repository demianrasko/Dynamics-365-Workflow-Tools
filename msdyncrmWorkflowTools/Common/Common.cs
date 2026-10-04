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
        public ITracingService TracingService;
        public IWorkflowContext Context;
        public IOrganizationServiceFactory ServiceFactory;
        public IOrganizationService Service;

        /// <summary>
        /// Used by the workflow activities: pulls the tracing service, workflow context and organization service from the execution context.
        /// </summary>
        public Common(CodeActivityContext executionContext)
        {
            TracingService = executionContext.GetExtension<ITracingService>();
            Context = executionContext.GetExtension<IWorkflowContext>();
            ServiceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            Service = ServiceFactory.CreateOrganizationService(Context.UserId);
        }

        /// <summary>
        /// Used by unit tests and the console app, where no workflow execution context exists.
        /// </summary>
        public Common(IOrganizationService service, ITracingService tracingService = null, IWorkflowContext context = null)
        {
            Service = service;
            TracingService = tracingService ?? new NullTracingService();
            Context = context;
        }

        /// <summary>
        /// Writes a message to the trace log exactly as given, so it may contain { and } (interpolated
        /// values, JSON, FetchXML, stack traces).
        /// </summary>
        public void Trace(string message)
        {
            TracingService.Trace("{0}", message);
        }

        /// <summary>
        /// Writes a composite-format message (string.Format style) to the trace log.
        /// </summary>
        public void Trace(string format, params object[] args)
        {
            TracingService.Trace(format, args);
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

            var response = (RetrieveMetadataChangesResponse)Service.Execute(request);

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

            return Service.RetrieveMultiple(Queries.Associations(primaryEntityName, primaryEntityId, intersectEntityName, entityName, new Guid(parentId)));
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
                response = (RetrieveRelationshipResponse)Service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });
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
                var page = Service.RetrieveMultiple(query);
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
            var response = (FetchXmlToQueryExpressionResponse)Service.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml });
            return response.Query;
        }

        /// <summary>
        /// Labels of an option set (choice) or multi-select option set attribute, by value, in the user's language.
        /// </summary>
        public Dictionary<int, string> GetOptionSetLabels(string entityName, string attributeName)
        {
            var response = (RetrieveAttributeResponse)Service.Execute(new RetrieveAttributeRequest
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
                var page = Service.RetrieveMultiple(query);
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
        /// Returns every record a FetchXML query returns, page by page.
        /// </summary>
        /// <param name="fetchXml">The fetch query, without paging attributes.</param>
        /// <param name="pageSize">Records per page.</param>
        public List<Entity> RetrieveAllWithFetchXml(string fetchXml, int pageSize = 250)
        {
            var records = new List<Entity>();
            string pagingCookie = null;

            for (var pageNumber = 1; ; pageNumber++)
            {
                var page = Service.RetrieveMultiple(new FetchExpression(Utility.CreateXml(fetchXml, pagingCookie, pageNumber, pageSize)));
                records.AddRange(page.Entities);

                if (!page.MoreRecords)
                {
                    return records;
                }

                pagingCookie = page.PagingCookie;
            }
        }

        /// <summary>
        /// Starts an on-demand workflow for each record.
        /// </summary>
        public void ExecuteWorkflow(Guid workflowId, IEnumerable<Guid> recordIds)
        {
            foreach (var recordId in recordIds)
            {
                Service.Execute(new ExecuteWorkflowRequest { EntityId = recordId, WorkflowId = workflowId });
            }
        }

        /// <summary>
        /// Ids of the records on the "many" side of a 1:N relationship whose lookup points at <paramref name="parentId"/>.
        /// </summary>
        public List<Guid> GetOneToManyRelatedIds(string relationshipName, Guid parentId)
        {
            var response = (RetrieveRelationshipResponse)Service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });

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
            var response = (RetrieveRelationshipResponse)Service.Execute(new RetrieveRelationshipRequest { Name = relationshipName, RetrieveAsIfPublished = false });

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

            return RetrieveAllIds(relationship.Entity1LogicalName == primaryEntityName 
                ? Queries.ManyToManyRelated(relationship.Entity2LogicalName, relationship.Entity2IntersectAttribute, relationship.Entity2IntersectAttribute, intersect, relationship.Entity1IntersectAttribute, primaryEntityId) 
                : Queries.ManyToManyRelated(relationship.Entity1LogicalName, relationship.Entity1IntersectAttribute, relationship.Entity1IntersectAttribute, intersect, relationship.Entity2IntersectAttribute, primaryEntityId));
        }

        public List<string> GetEntityAttributesToClone(string entityName, ref string primaryIdAttribute, ref string primaryNameAttribute)
        {
            var attributes = new List<string>();
            var request = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            };

            var response = (RetrieveEntityResponse)Service.Execute(request);
            primaryIdAttribute = response.EntityMetadata.PrimaryIdAttribute;

            foreach (var attMetadata in response.EntityMetadata.Attributes)
            {
                if (attMetadata.IsPrimaryName != null && attMetadata.IsPrimaryName.Value)
                {
                    primaryNameAttribute = attMetadata.LogicalName;
                }

                if (attMetadata.IsValidForCreate != null &&
                    attMetadata.IsValidForUpdate != null &&
                    attMetadata.IsPrimaryId != null &&
                    ((!attMetadata.IsValidForCreate.Value && 
                      !attMetadata.IsValidForUpdate.Value)
                     || attMetadata.IsPrimaryId.Value))
                {
                    continue;
                }

                attributes.Add(attMetadata.AttributeTypeName.Value.ToLower() == "partylisttype"
                    ? $"partylist-{attMetadata.LogicalName}"
                    : attMetadata.LogicalName);
            }

            return attributes;
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

            var retrievedObject = Service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            Trace("retrieved object OK");

            var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;

            var attributesToClone = GetEntityAttributesToClone(entityName, ref primaryIdAttribute, ref primaryNameAttribute);

            foreach (var attribute in attributesToClone)
            {
                if (!string.IsNullOrEmpty(fieldstoIgnore))
                {
                    if (Array.IndexOf(fieldstoIgnore.Split(';'), attribute) >= 0 || Array.IndexOf(fieldstoIgnore.Split(','), attribute) >= 0)
                    {
                        continue;
                    }
                }

                if ((!retrievedObject.Attributes.Contains(attribute) || attribute == "statuscode" || attribute == "statecode")
                    && !attribute.StartsWith("partylist-"))
                {
                    continue;
                }

                var newPartyList = new EntityCollection();

                if (attribute.StartsWith("partylist-"))
                {
                    var attribute2 = attribute.Replace("partylist-", string.Empty);

                    var participationTypeMask = Utility.GetParticipation(attribute2);
                    if (string.IsNullOrEmpty(participationTypeMask))
                    {
                        throw new InvalidPluginExecutionException($"Unsupported party list attribute '{attribute2}'.");
                    }

                    var returnCollection = Service.RetrieveMultiple(
                        Queries.ActivityParties(new Guid(objectId), int.Parse(participationTypeMask)));

                    Trace("attribute:{0}", attribute2);

                    foreach (var ent in returnCollection.Entities)
                    {
                        var partyid = (EntityReference)ent.Attributes["partyid"];

                        // one activityparty per party (re-using one entity threw "same key" for a second party)
                        var party = new Entity("activityparty")
                        {
                            ["partyid"] = new EntityReference(partyid.LogicalName, partyid.Id)
                        };

                        Trace("attribute:{0}:{1}:{2}", attribute2, partyid.LogicalName, partyid.Id.ToString());

                        newPartyList.Entities.Add(party);
                    }

                    newEntity.Attributes.Add(attribute2, newPartyList);
                    continue;
                }

                Trace("attribute:{0}", attribute);

                if (attribute == primaryNameAttribute && prefix != null)
                {
                    retrievedObject.Attributes[attribute] = prefix + retrievedObject.Attributes[attribute];
                }

                newEntity.Attributes.Add(attribute, retrievedObject.Attributes[attribute]);
            }

            Trace("creating cloned object...");
            var id = Service.Create(newEntity);
            Trace("created cloned object OK");

            if (newEntity.Attributes.Contains("statuscode") && newEntity.Attributes.Contains("statecode"))
            {
                var record = Service.Retrieve(entityName, id, new ColumnSet("statuscode", "statecode"));

                if (retrievedObject.Attributes["statuscode"] != record.Attributes["statuscode"] ||
                    retrievedObject.Attributes["statecode"] != record.Attributes["statecode"])
                {
                    var setStatusEnt = new Entity(entityName, id);
                    setStatusEnt.Attributes.Add("statuscode", retrievedObject.Attributes["statuscode"]);
                    setStatusEnt.Attributes.Add("statecode", retrievedObject.Attributes["statecode"]);

                    Service.Update(setStatusEnt);
                }
            }

            Trace("cloned object OK");

            return id;
        }

        public void DeleteRecordAuditHistory(string logicalName, string id)
        {
            var request = new DeleteRecordChangeHistoryRequest();

            var entityReference = new EntityReference(logicalName, new Guid(id));

            request.Target = entityReference;
            Service.Execute(request);
        }

        public EntityReference RetrieveUserBuDefaultTeam(string systemUserId)
        {
            // TODO: Cleanup after testing
            //var teamReference = new EntityReference("team");

            var team = Service.RetrieveMultiple(Queries.DefaultTeamForUser(new Guid(systemUserId)));

            //teamReference.Id = team.Entities[0].Id;

            return team?.Entities[0].ToEntityReference();
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
        }*/

        public bool SendEmailToUsersInRole(EntityReference securityRoleLookup, EntityReference emailReference)
        {
            var userList = Service.RetrieveMultiple(Queries.UsersInRole(securityRoleLookup.Id));
            Trace("Retrieved Data");

            var email = new Entity("email", emailReference.Id);

            var to = new EntityCollection();

            foreach (var user in userList.Entities)
            {
                var userId = user.Id;

                var to1 = new Entity("activityparty")
                {
                    ["partyid"] = new EntityReference("systemuser", userId)
                };

                to.Entities.Add(to1);
            }

            email["to"] = to;

            Service.Update(email);

            var request = new SendEmailRequest
            {
                EmailId = emailReference.Id
            };

            var response = (SendEmailResponse)Service.Execute(request);

            //TODO: Investigate
            return true;
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

            // TODO: Verify this is the functionality we want
            if (failures.Count > 0)
            {
                Trace($"The email could not be sent to {failures.Count} of {userList.Entities.Count} users. {string.Join("; ", failures)}");
            }

            return true;
        }

        public bool SendEmailFromTemplate(EntityReference template, Guid userId)
        {
            var toEntities = new List<Entity>();
            var activityParty = new Entity
            {
                LogicalName = "activityparty",
                Attributes =
                {
                    ["partyid"] = new EntityReference("systemuser", userId)
                }
            };

            toEntities.Add(activityParty);

            var email = new Entity("email")
            {
                Attributes =
                {
                    ["to"] = toEntities.ToArray()
                }
            };

            var request = new SendEmailFromTemplateRequest
            {
                Target = email,

                // Use a built-in Email Template of type "contact".
                TemplateId = template.Id,

                // The regarding Id is required, and must be of the same type as the Email Template.
                RegardingId = userId,
                RegardingType = "systemuser"
            };

            var response = (SendEmailFromTemplateResponse)Service.Execute(request);

            // TODO: Investigate
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

            var collection = Service.RetrieveMultiple(query).Entities;

            return collection.First()["appmoduleid"].ToString();
        }

        public string GetAppRecordUrl(string recordUrl, string appModuleUniqueName)
        {
            var appModuleId = GetAppModuleId(appModuleUniqueName);

            return $"{recordUrl}&appid={appModuleId}";
        }

        /// <summary>
        /// The SharePoint document locations of a record.
        /// </summary>
        /// <param name="regardingObjectId">The record whose document locations are returned.</param>
        public EntityCollection GetSharepointLocations(Guid regardingObjectId)
        {
            return Service.RetrieveMultiple(Queries.SharepointDocumentLocations(regardingObjectId));
        }

        /// <summary>
        /// The absolute SharePoint URL of the first document location in <paramref name="locations"/>.
        /// </summary>
        /// <param name="locations">Document locations, e.g. from <see cref="GetSharepointLocations"/>.</param>
        /// <returns>The absolute URL, or "URL Not found" when there are no locations.</returns>
        public string GetAbsoluteUrlFromLocation(EntityCollection locations)
        {
            if (locations.Entities.Count == 0)
            {
                return "URL Not found";
            }

            var request = new RetrieveAbsoluteAndSiteCollectionUrlRequest
            {
                Target = locations[0].ToEntityReference()
            };

            var response = (RetrieveAbsoluteAndSiteCollectionUrlResponse)Service.Execute(request);

            Trace($"Absolute URL of document location record is '{response.AbsoluteUrl}'.");

            return response.AbsoluteUrl;
        }

        /// <summary>
        /// Recalculates a goal now instead of waiting for the server's scheduled recalculation.
        /// </summary>
        /// <param name="goalId">The goal to recalculate.</param>
        public void RecalculateGoal(Guid goalId)
        {
            Trace($"Recalculating goal {goalId}");

            var request = new RecalculateRequest
            {
                Target = new EntityReference("goal", goalId)
            };

            Service.Execute(request);
        }

        /// <summary>
        /// Whether a record (account, contact or lead) is a member of a marketing list.
        /// </summary>
        /// <param name="listId">The marketing list.</param>
        /// <param name="memberId">The record to look for.</param>
        public bool IsMemberOfMarketingList(Guid listId, Guid memberId)
        {
            return Service.RetrieveMultiple(Queries.MarketingListMembership(listId, memberId)).Entities.Count > 0;
        }

        /// <summary>
        /// Shares a secured (field security) field of a record with users and teams, updates their existing access,
        /// or removes it when both <paramref name="allowRead"/> and <paramref name="allowUpdate"/> are false.
        /// Does nothing when the field is not secured.
        /// </summary>
        /// <param name="record">The record whose field is shared.</param>
        /// <param name="attributeName">Logical name of the secured field.</param>
        /// <param name="allowRead">Grant read access.</param>
        /// <param name="allowUpdate">Grant update access.</param>
        /// <param name="principals">The systemuser and team references to share with; nulls are skipped.</param>
        public void ShareSecuredField(EntityReference record, string attributeName, bool allowRead, bool allowUpdate, params EntityReference[] principals)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = record.LogicalName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };

            var response = (RetrieveAttributeResponse)Service.Execute(request);
            var attribute = response.AttributeMetadata;

            if (attribute?.IsSecured != true || attribute.MetadataId == null)
            {
                Trace($"{record.LogicalName}.{attributeName} is not a secured field; nothing to share.");
                return;
            }

            foreach (var principal in principals.Where(p => p != null))
            {
                var existing = Service.RetrieveMultiple(Queries.FieldSharing(attribute.MetadataId.Value, record.Id, principal.Id)).Entities.FirstOrDefault();

                if (existing != null)
                {
                    if (allowRead || allowUpdate)
                    {
                        existing["readaccess"] = allowRead;
                        existing["updateaccess"] = allowUpdate;

                        Service.Update(existing);
                    }
                    else
                    {
                        Service.Delete(existing.LogicalName, existing.Id);
                    }

                    continue;
                }

                if (!allowRead && !allowUpdate)
                {
                    continue;
                }

                Service.Create(new Entity("principalobjectattributeaccess")
                {
                    ["attributeid"] = attribute.MetadataId.Value,
                    ["objectid"] = record,
                    ["principalid"] = principal,
                    ["readaccess"] = allowRead,
                    ["updateaccess"] = allowUpdate
                });
            }
        }

        /// <summary>
        /// Sets a multi-select option set field on a record, optionally keeping the values it already has.
        /// </summary>
        /// <param name="target">The record to update.</param>
        /// <param name="attributeName">Logical name of the multi-select option set field.</param>
        /// <param name="values">The values to set; an empty collection clears the field unless existing values are kept.</param>
        /// <param name="keepExistingValues">Add <paramref name="values"/> to the current values instead of replacing them.</param>
        public void SetMultiSelectOptionSet(EntityReference target, string attributeName, OptionSetValueCollection values, bool keepExistingValues)
        {
            SetMultiSelectOptionSets(target, new Dictionary<string, OptionSetValueCollection> { [attributeName] = values }, keepExistingValues);
        }

        /// <summary>
        /// Sets several multi-select option set fields on a record in one update, optionally keeping the values
        /// they already have (read in one retrieve).
        /// </summary>
        /// <param name="target">The record to update.</param>
        /// <param name="values">The values to set, by field logical name.</param>
        /// <param name="keepExistingValues">Add the values to the current values instead of replacing them.</param>
        public void SetMultiSelectOptionSets(EntityReference target, IDictionary<string, OptionSetValueCollection> values, bool keepExistingValues)
        {
            if (values.Count == 0)
            {
                Trace("No multi-select option set values to set.");
                return;
            }

            var existing = keepExistingValues
                ? Service.Retrieve(target.LogicalName, target.Id, new ColumnSet(values.Keys.ToArray()))
                : null;

            var update = new Entity(target.LogicalName, target.Id);

            foreach (var field in values)
            {
                update[field.Key] = existing == null
                    ? field.Value
                    : Utility.MergeOptionSetValues(field.Value, existing.GetAttributeValue<OptionSetValueCollection>(field.Key));

                Trace($"Multi-select option set '{field.Key}' on {target.LogicalName} {target.Id}: {((OptionSetValueCollection)update[field.Key]).Count} value(s).");
            }

            Service.Update(update);
        }

        /// <summary>
        /// Copies multi-select option set fields from one record to another: the n-th source field to the n-th
        /// target field. Source fields that are empty or are not multi-select option sets are skipped.
        /// </summary>
        /// <param name="source">The record to copy from.</param>
        /// <param name="sourceAttributes">Source field logical names.</param>
        /// <param name="target">The record to copy to.</param>
        /// <param name="targetAttributes">Target field logical names, in the same order as <paramref name="sourceAttributes"/>.</param>
        /// <param name="keepExistingValues">Add the copied values to the target's current values instead of replacing them.</param>
        public void MapMultiSelectOptionSets(EntityReference source, string[] sourceAttributes, EntityReference target, string[] targetAttributes, bool keepExistingValues)
        {
            if (sourceAttributes.Length != targetAttributes.Length)
            {
                throw new InvalidPluginExecutionException(
                    $"The number of source attributes ({sourceAttributes.Length}) does not match the number of target attributes ({targetAttributes.Length}).");
            }

            var sourceRecord = Service.Retrieve(source.LogicalName, source.Id, new ColumnSet(sourceAttributes));
            var values = new Dictionary<string, OptionSetValueCollection>();

            for (var i = 0; i < sourceAttributes.Length; i++)
            {
                if (sourceRecord.GetAttributeValue<object>(sourceAttributes[i]) is OptionSetValueCollection sourceValues)
                {
                    values[targetAttributes[i]] = sourceValues;
                }
                else
                {
                    Trace($"Source attribute '{sourceAttributes[i]}' is empty or not a multi-select option set; skipped.");
                }
            }

            SetMultiSelectOptionSets(target, values, keepExistingValues);
        }

        /// <summary>
        /// Moves a record's business process flow instance to the named stage.
        /// </summary>
        /// <param name="record">The record the process runs on.</param>
        /// <param name="process">The business process flow (workflow) the stage belongs to.</param>
        /// <param name="stageName">The stage name, as shown in the process.</param>
        public void SetProcessStage(EntityReference record, EntityReference process, string stageName)
        {
            var stageId = GetProcessStageId(process.Id, stageName);
            var instance = GetProcessInstance(record, process.Id);
            var instanceEntityName = GetProcessEntityName(process.Id);

            Trace($"Moving {instanceEntityName} {instance.Id} to stage '{stageName}' ({stageId}).");

            Service.Update(new Entity(instanceEntityName, instance.Id)
            {
                ["activestageid"] = new EntityReference("processstage", stageId)
            });
        }

        /// <summary>
        /// The id of the stage named <paramref name="stageName"/> in a business process flow.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The process has no stage with that name.</exception>
        public Guid GetProcessStageId(Guid processId, string stageName)
        {
            var stage = Service.RetrieveMultiple(Queries.ProcessStage(processId, stageName)).Entities.FirstOrDefault();

            if (stage == null)
            {
                throw new InvalidPluginExecutionException($"Process stage '{stageName}' was not found in process {processId}.");
            }

            return stage.Id;
        }

        /// <summary>
        /// The record's instance of a business process flow. When the instances do not say which process they
        /// belong to, the record's active instance (the first one returned) is used.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The record has no instance of the process.</exception>
        public Entity GetProcessInstance(EntityReference record, Guid processId)
        {
            var request = new RetrieveProcessInstancesRequest
            {
                EntityId = record.Id,
                EntityLogicalName = record.LogicalName
            };

            var response = (RetrieveProcessInstancesResponse)Service.Execute(request);
            var instances = response.Processes.Entities;

            var instance = instances.Any(i => i.Contains("processid"))
                ? instances.FirstOrDefault(i => i.GetAttributeValue<EntityReference>("processid")?.Id == processId)
                : instances.FirstOrDefault();

            if (instance == null)
            {
                throw new InvalidPluginExecutionException($"No instance of process {processId} was found for {record.LogicalName} {record.Id}.");
            }

            Trace($"Process instance: '{instance.GetAttributeValue<string>("name")}' ({instance.Id})");

            return instance;
        }

        /// <summary>
        /// The logical name of the entity that stores a business process flow's instances (the process unique name).
        /// </summary>
        public string GetProcessEntityName(Guid processId)
        {
            return Service.Retrieve("workflow", processId, new ColumnSet("uniquename")).GetAttributeValue<string>("uniquename");
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

            var retrievedUsers = Service.RetrieveMultiple(query);

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
            var principalRecord = Service.Retrieve(principal.LogicalName, principal.Id, new ColumnSet("businessunitid"));
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

            var givenRoles = Service.RetrieveMultiple(roleQuery);

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

            var businessUnitRoles = Service.RetrieveMultiple(businessUnitRoleQuery);

            return (Guid)businessUnitRoles.Entities[0].Attributes["roleid"];
        }

        /// <summary>
        /// Parses a record URL and fills in its entity name: from the URL's "etn" parameter when it has one,
        /// otherwise by looking up the "etc" type code in the metadata.
        /// </summary>
        /// <param name="recordUrl">The record URL a workflow passes in.</param>
        /// <returns>The object type code, record id and entity logical name.</returns>
        public RecordUrl ParseRecordUrl(string recordUrl)
        {
            var parsedUrl = Utility.ParseRecordUrl(recordUrl);

            if (!string.IsNullOrEmpty(parsedUrl.EntityName))
            {
                return parsedUrl;
            }

            return new RecordUrl(parsedUrl.ObjectTypeCode, parsedUrl.Id, GetEntityNameFromCode(parsedUrl.ObjectTypeCode));
        }

        /// <summary>
        /// The record a record URL points at, with the entity name looked up from the URL's type code.
        /// </summary>
        public EntityReference GetRecordReference(string recordUrl)
        {
            var parsedUrl = ParseRecordUrl(recordUrl);

            Trace($"ObjectTypeCode={parsedUrl.ObjectTypeCode}--ParentId={parsedUrl.Id}");

            return new EntityReference(parsedUrl.EntityName, new Guid(parsedUrl.Id));
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
                Service.Execute(new GrantAccessRequest
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
                Service.Execute(new RevokeAccessRequest { Target = target, Revokee = principal });
            }

            Trace("Revoked Permissions--- OK");
        }

        public void DeleteOptionValue(bool globalOptionSet, string attributeName, string entityName, int optionValue)
        {
            var request = new DeleteOptionValueRequest
            {
                Value = optionValue
            };

            if (globalOptionSet)
            {
                request.OptionSetName = attributeName;
            }
            else
            {
                request.AttributeLogicalName = attributeName;
                request.EntityLogicalName = entityName;
            }

            Service.Execute(request);
        }

        public void SalesLiteratureToEmail(string fileName, string salesLiteratureId, string emailid)
        {
            if (fileName == "*")
            {
                fileName = string.Empty;
            }

            fileName = fileName.Replace("*", "%");

            #region "Query Attachments"
            var fileNamePattern = $"%{fileName}%";
            Trace($"Sales literature items: file name like '{fileNamePattern}', sales literature {salesLiteratureId}");

            var attachmentFiles = Service.RetrieveMultiple(Queries.SalesLiteratureItems(fileNamePattern, new Guid(salesLiteratureId)));

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
                var attachment = new Entity("activitymimeattachment")
                {
                    ["objectid"] = new EntityReference("email", new Guid(emailid)),
                    ["objecttypecode"] = "email",
                    ["attachmentnumber"] = i
                };

                i++;

                Utility.CopyAttributeValue(file, "title", attachment, "subject");
                Utility.CopyAttributeValue(file, "filename", attachment);
                Utility.CopyAttributeValue(file, "documentbody", attachment, "body");
                Utility.CopyAttributeValue(file, "mimetype", attachment);

                Service.Create(attachment);
            }

            #endregion
        }

        public bool InsertOptionValue(bool globalOptionSet, string attributeName, string entityName, string optionText, int optionValue, int languageCode)
        {
            var request = new InsertOptionValueRequest
            {
                Value = optionValue,
                Label = new Label(optionText, languageCode)
            };

            if (globalOptionSet)
            {
                request.OptionSetName = attributeName;
            }
            else
            {
                request.AttributeLogicalName = attributeName;
                request.EntityLogicalName = entityName;
            }

            Service.Execute(request);

            return true;
        }

        public Guid CreateTeam(string teamName, int teamType, EntityReference administrator, EntityReference businessUnit)
        {
            var team = new Entity("team")
            {
                ["administratorid"] = administrator,
                ["name"] = teamName,
                ["teamtype"] = new OptionSetValue(teamType),
                ["businessunitid"] = businessUnit
            };

            return Service.Create(team);
        }

        public void AssociateEntity(string primaryEntityName, Guid primaryEntityId, string relationshipName, string relationshipEntityName, string entityName, string parentId)
        {
            try
            {
                var relations = GetAssociations(primaryEntityName, primaryEntityId, relationshipEntityName, entityName, parentId);

                if (relations.Entities.Count != 0)
                {
                    return;
                }

                var relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(entityName, new Guid(parentId))
                };

                var relationship = new Relationship(relationshipName);

                if (primaryEntityName == entityName)
                {
                    relationship.PrimaryEntityRole = EntityRole.Referencing;
                }

                Service.Associate(primaryEntityName, primaryEntityId, relationship, relatedEntities);
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
            var attachmentFiles = Service.RetrieveMultiple(
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

                var attachment = new Entity("activitymimeattachment")
                {
                    ["objectid"] = new EntityReference("email", email.Id),
                    ["objecttypecode"] = "email",
                    ["attachmentnumber"] = i
                };
                i++;

                Utility.CopyAttributeValue(file, "subject", attachment);
                Utility.CopyAttributeValue(file, "filename", attachment);
                Utility.CopyAttributeValue(file, "mimetype", attachment);

                if (!Utility.CopyAttributeValue(file, "documentbody", attachment, "body"))
                {
                    Utility.CopyAttributeValue(file, "body", attachment);
                }

                if (mostRecent)
                {
                    Trace("Is Most Recent");

                    var alreadyAttached = attachedFiles.Where(f => f["filename"].ToString() == file.GetAttributeValue<string>("filename")).FirstOrDefault();

                    if (alreadyAttached == null)
                    {
                        Trace("not already attached");

                        Service.Create(attachment);

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
                    Service.Create(attachment);
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
            var request = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };

            var response = (RetrieveRelationshipResponse)Service.Execute(request);
            var rel = (OneToManyRelationshipMetadata)response.RelationshipMetadata;
            var childEntityType = rel.ReferencingEntity;
            var childEntityFieldName = rel.ReferencingAttribute;

            var query = new QueryByAttribute(childEntityType)
            {
                ColumnSet = new ColumnSet(childEntityFieldName),
                Attributes = { childEntityFieldName },
                Values = { new Guid(parentEntityId) }
            };

            return Service.RetrieveMultiple(query);
        }

        public void UpdateChildRecords(string relationshipName, string parentEntityType, string parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate, bool updateonlyActive)
        {
            //1) Get child lookup field name
            var req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };

            var res = (RetrieveRelationshipResponse)Service.Execute(req);
            var rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            var childEntityType = rel.ReferencingEntity;
            var childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            var query = new QueryByAttribute(childEntityType)
            {
                ColumnSet = new ColumnSet(childEntityFieldName),
                Attributes = { childEntityFieldName },
                Values = { new Guid(parentEntityId) }
            };

            if (updateonlyActive)
            {
                query.AddAttributeValue("statecode", 0);
            }

            var retrieved = Service.RetrieveMultiple(query);

            //2') retrieve parent field value
            object valueToUpdate;

            if (!string.IsNullOrEmpty(parentFieldNameToUpdate))
            {
                var retrievedEntity = Service.Retrieve(parentEntityType, new Guid(parentEntityId), new ColumnSet(parentFieldNameToUpdate));

                valueToUpdate = retrievedEntity.Attributes.Contains(parentFieldNameToUpdate) ? retrievedEntity.Attributes[parentFieldNameToUpdate] : null;
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
                    var request = new UpdateProductPropertiesRequest();
                    // req2.
                    break;
                }

                var attributeRequest = new RetrieveAttributeRequest
                {
                    EntityLogicalName = childEntityType,
                    LogicalName = childFieldNameToUpdate
                };

                var attributeResponse = (RetrieveAttributeResponse)Service.Execute(attributeRequest);

                var metadata = attributeResponse.AttributeMetadata;

                var entity = new Entity(childEntityType)
                {
                    Id = child.Id
                };

                if (metadata.AttributeType != null)
                {
                    switch (metadata.AttributeType.Value.ToString())
                    {
                        case "Boolean":
                        {
                            // valueToUpdate is an object, so compare its text (== "1" compared references and missed "1" read from a field)
                            var text = Convert.ToString(valueToUpdate, CultureInfo.InvariantCulture);
                            var isTrue = valueToUpdate is bool flag
                                ? flag
                                : text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);

                            entity.Attributes.Add(childFieldNameToUpdate, isTrue);
                            break;
                        }
                        case "Picklist":
                        case "Status":
                        {
                            if (valueToUpdate == null)
                            {
                                entity.Attributes.Add(childFieldNameToUpdate, null);
                            }
                            else
                            {
                                if (valueToUpdate is OptionSetValue value)
                                {
                                    valueToUpdate = value.Value;
                                }

                                var opt = new OptionSetValue(Convert.ToInt32(valueToUpdate));
                                entity.Attributes.Add(childFieldNameToUpdate, opt);
                            }

                            break;
                        }
                        default:
                        {
                            entity.Attributes.Add(childFieldNameToUpdate, valueToUpdate);
                            break;
                        }
                    }
                }

                Service.Update(entity);
            }
        }
    }
}