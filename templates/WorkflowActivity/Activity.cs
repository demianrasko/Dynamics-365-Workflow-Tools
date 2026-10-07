using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// TODO: One sentence on what the activity does.
    /// </summary>
    // the name in the workflow designer; every activity needs one, and it must be unique
    [ActivityName("TODO: Designer Name")]
    public class $safeitemname$ : WorkflowActivityBase
    {
        // Inputs and outputs are what workflows store. Once an activity is released, never rename the
        // class or these properties, or change their types: existing workflows would break. Adding a new
        // optional (not [RequiredArgument]) input is fine.
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [Output("Result")]
        public OutArgument<string> Result { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            // Only read the inputs, call one Common method and set the outputs: the checks and the work go in the
            // Common method, so they can be tested.
            Result.Set(executionContext, common.$safeitemname$(RecordURL.Get(executionContext)));
        }
    }
}
