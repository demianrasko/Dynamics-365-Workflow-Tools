// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class SalesLiteratureToEmail : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Sales Literature")]
        [ReferenceTarget(EntityNames.SalesLiterature)]
        public InArgument<EntityReference> SalesLiterature { get; set; }

        [RequiredArgument]
        [Input("File Name (use * for filter)")]
        [ReferenceTarget("")]
        public InArgument<string> FileName { get; set; }

        [RequiredArgument]
        [Input("Email")]
        [ReferenceTarget(EntityNames.Email)]
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

            common.SalesLiteratureToEmail(fileName, salesLiterature.Id, email.Id);
        }
    }
}
#endif
