using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
namespace msdyncrmWorkflowTools.Class
{
    public class RemoveFromAllMarketingLists : CodeActivity
    {
       


        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            var context = executionContext.GetExtension<IWorkflowContext>();

            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"


            #endregion


            string entityName=(context.PrimaryEntityName);
            if (entityName != "account" && entityName != "contact" && entityName != "lead")
            {
                throw new Exception("MSG_UNSUPPORTED_MARKETING_LIST_MEMBER_TYPE");
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

            foreach (Entity member in listMembers)
            {
                EntityReference ent= (EntityReference)member.Attributes["entityid"];
                EntityReference list = (EntityReference)member.Attributes["listid"];
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
            var idColumnName = String.Format("{0}id", entityName);

            var query = new QueryByAttribute(entityName);

            query.AddAttributeValue(idColumnName, id);
            query.ColumnSet = new ColumnSet(new[] { idColumnName });
            query.PageInfo = new PagingInfo { Count = 1, PageNumber = 1, PagingCookie = null };

            var collection = service.RetrieveMultiple(query);

            return (collection.Entities.Count > 0);
        }

    }
}
