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

            Common objCommon = new Common(executionContext);
            var context = executionContext.GetExtension<IWorkflowContext>();
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            InitiatingUser.Set(executionContext, new EntityReference("systemuser", context.InitiatingUserId));

        }

     

    }
}
