using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class RemoveFromAllMarketingLists : WorkflowActivityBase
    {
        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Load CRM Service from context"

            var context = executionContext.GetExtension<IWorkflowContext>();

            objCommon.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            #endregion

            var entityName=context.PrimaryEntityName;

            if (entityName != "account" && entityName != "contact" && entityName != "lead")
            {
                throw new InvalidPluginExecutionException("Remove From All Marketing Lists only supports account, contact or lead records.");
            }
            
            if (!DoesCrmRecordExist(objCommon.service, context.PrimaryEntityName, context.PrimaryEntityId))
            {
                return;
            }

            var query = new QueryExpression
            {
                EntityName = "listmember",
                ColumnSet = new ColumnSet("entityid", "listid"),

                Criteria =
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                                            {
                                                new ConditionExpression
                                                {
                                                    AttributeName = "entityid",
                                                    Operator = ConditionOperator.Equal,
                                                    Values = { context.PrimaryEntityId }
                                                },
                                            }
                    }
            };

            var listMembers = objCommon.service.RetrieveMultiple(query).Entities;

            foreach (var member in listMembers)
            {
                var ent= (EntityReference)member.Attributes["entityid"];
                var list = (EntityReference)member.Attributes["listid"];
            
                var request = new RemoveMemberListRequest
                {
                    EntityId = ent.Id,
                    ListId = list.Id
                };

                objCommon.service.Execute(request);
            }
        }

        public bool DoesCrmRecordExist(IOrganizationService service, string entityName, Guid id)
        {
            var idColumnName = $"{entityName}id";

            var query = new QueryByAttribute(entityName);

            query.AddAttributeValue(idColumnName, id);
            query.ColumnSet = new ColumnSet(idColumnName);
            query.PageInfo = new PagingInfo { Count = 1, PageNumber = 1, PagingCookie = null };

            var collection = service.RetrieveMultiple(query);

            return (collection.Entities.Count > 0);
        }
    }
}
