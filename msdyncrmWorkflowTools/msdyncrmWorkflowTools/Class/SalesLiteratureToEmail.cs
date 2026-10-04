using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class SalesLiteratureToEmail : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"

            var salesLiterature = SalesLiterature.Get(executionContext);

            var fileName = FileName.Get(executionContext);
            if (string.IsNullOrEmpty(fileName))
            {
                throw new InvalidPluginExecutionException("File Name (use * for filter) is required.");
            }

            var email = Email.Get(executionContext);

            #endregion


            common.SalesLiteratureToEmail(fileName, salesLiterature.Id.ToString(), email.Id.ToString());
        }
    }
}
