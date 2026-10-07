using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Entity JSON Serializer")]
    public class EntityJsonSerializer : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Serializing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SerializingRecordURL { get; set; }

        [Output("Output Json")] public OutArgument<string> OutputJson { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            OutputJson.Set(executionContext, common.SerializeRecord(common.GetRecordReference(SerializingRecordURL.Get(executionContext), "Serializing Record URL")));
        }
    }
}
