using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class EntityJsonSerializer : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Serializing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SerializingRecordURL { get; set; }

        [Output("Output Json")] public OutArgument<string> OutputJson { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = SerializingRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Serializing Record URL is required.");
            }

            OutputJson.Set(executionContext, common.SerializeRecord(common.GetRecordReference(recordUrl)));
        }
    }
}
