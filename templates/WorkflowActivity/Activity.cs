using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// TODO: One sentence on what the activity does.
    /// </summary>
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
            // Read and check the inputs here; put the Dataverse work in a Common method so it can be tested.
            var recordUrl = RecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var result = common.$safeitemname$(common.GetRecordReference(recordUrl));

            Result.Set(executionContext, result);
        }
    }
}
