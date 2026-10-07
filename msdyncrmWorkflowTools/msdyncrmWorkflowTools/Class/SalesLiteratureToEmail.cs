// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Sales Literature To Email")]
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
            common.SalesLiteratureToEmail(FileName.Get(executionContext), SalesLiterature.Get(executionContext).Id, Email.Get(executionContext).Id);
        }
    }
}
#endif
