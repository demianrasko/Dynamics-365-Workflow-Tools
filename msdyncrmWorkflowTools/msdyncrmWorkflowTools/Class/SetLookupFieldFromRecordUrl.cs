using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Set Lookup Field From Record URL")]
    public class SetLookupFieldFromRecordUrl : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordUrl { get; set; }

        [RequiredArgument]
        [Input("Lookup Field Name")]
        public InArgument<string> LookupFieldName { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            // the record the URL points at becomes the lookup value on the workflow's primary record
            common.SetLookup(common.PrimaryRecord, Utility.Required(LookupFieldName.Get(executionContext), "Lookup Field Name"),
                common.GetRecordReference(RecordUrl.Get(executionContext), "Record URL"));
        }
    }
}
