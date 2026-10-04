using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class SalesLiteratureToEmail : CodeActivity
    {
        [RequiredArgument]
        [Input("Sales Literature")]
        [ReferenceTarget("salesliterature")]
        public InArgument<EntityReference> SalesLiterature { get; set; }

        [RequiredArgument]
        [Input("File Name (use * for filter)")]
        [ReferenceTarget("")]
        public InArgument<string> FileName { get; set; }

        [RequiredArgument]
        [Input("Email")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

            var salesLiterature = SalesLiterature.Get(executionContext);

            var fileName = FileName.Get(executionContext);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var email = Email.Get(executionContext);

            #endregion


            objCommon.SalesLiteratureToEmail(fileName, salesLiterature.Id.ToString(), email.Id.ToString());
        }
    }
}
