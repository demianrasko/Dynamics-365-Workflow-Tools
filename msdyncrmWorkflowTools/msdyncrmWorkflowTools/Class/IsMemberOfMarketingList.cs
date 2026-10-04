using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class IsMemberOfMarketingList : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Output("IsMemberOfMarketingList")]
        public OutArgument<bool> MemberOfMarketingList
        {
            get;
            set;
        }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Load CRM Service from context"

            var context = executionContext.GetExtension<IWorkflowContext>();
            common.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            common.Trace($"marketingList: {marketingList.Id.ToString()} ");
            #endregion

            var isMember = CheckIsMemberOfMarketingList(common.Service, marketingList.Id, context.PrimaryEntityId);

            MemberOfMarketingList.Set(executionContext, isMember);
        }

        public bool CheckIsMemberOfMarketingList(IOrganizationService service, Guid list, Guid id)
        {
            var query = new QueryExpression { EntityName = "list" };

            var linkEntity = new LinkEntity
            {
                JoinOperator = JoinOperator.Natural,
                LinkFromEntityName = "list",
                LinkFromAttributeName = "listid",
                LinkToEntityName = "listmember",
                LinkToAttributeName = "listid",
                LinkCriteria = new FilterExpression(LogicalOperator.And)
            };

            linkEntity.LinkCriteria.AddCondition("listid", ConditionOperator.Equal, list);
            linkEntity.LinkCriteria.AddCondition("entityid", ConditionOperator.Equal, id);

            query.LinkEntities.Add(linkEntity);

            var collection = service.RetrieveMultiple(query);

            return (collection.Entities.Count > 0);
        }
        public void CheckMarketingListMemberEntityType(string entityName)
        {
            if (entityName != "account" && entityName != "contact" && entityName != "lead")
            {
                throw new InvalidPluginExecutionException("Entity type error. Must be account, contact or lead.");
            }
        }
    }
}
