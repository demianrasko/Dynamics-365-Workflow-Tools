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
    public class IsMemberOfMarketingList : CodeActivity
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



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            var context = executionContext.GetExtension<IWorkflowContext>();
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference marketingList = this.MarketingList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", marketingList.Id.ToString()));


            #endregion


            var isMember = CheckIsMemberOfMarketingList(objCommon.service,
                                                               marketingList.Id,
                                                               context.PrimaryEntityId);

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
                throw new Exception("Entity type error. Must be account, contact or lead.");
            }
        }

    }
}
