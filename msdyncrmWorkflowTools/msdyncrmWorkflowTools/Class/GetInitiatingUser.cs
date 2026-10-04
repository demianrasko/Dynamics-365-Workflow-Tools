using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
namespace msdyncrmWorkflowTools.Class
{
    public class GetInitiatingUser : WorkflowActivityBase
    {
       

        [Output("Initiating User")]
        [ReferenceTarget("systemuser")]
        public OutArgument<EntityReference> InitiatingUser
        {
            get;
            set;
        }



        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Load CRM Service from context"

            var context = executionContext.GetExtension<IWorkflowContext>();
            objCommon.Trace("Load CRM Service from context --- OK");
            #endregion

            InitiatingUser.Set(executionContext, new EntityReference("systemuser", context.InitiatingUserId));

        }

     

    }
}
