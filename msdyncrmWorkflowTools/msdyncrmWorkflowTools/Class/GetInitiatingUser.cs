using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class GetInitiatingUser : CodeActivity
    {
       

        [Output("Initiating User")]
        [ReferenceTarget("systemuser")]
        public OutArgument<EntityReference> InitiatingUser
        {
            get;
            set;
        }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            var context = executionContext.GetExtension<IWorkflowContext>();
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            InitiatingUser.Set(executionContext, new EntityReference("systemuser", context.InitiatingUserId));

        }

     

    }
}
