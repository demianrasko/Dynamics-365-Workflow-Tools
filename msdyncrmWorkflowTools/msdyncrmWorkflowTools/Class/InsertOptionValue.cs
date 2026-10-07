using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Insert Option Value")]
    public class InsertOptionValue : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Global Option Set")]
        [Default("false")]
        public InArgument<bool> GlobalOptionSet { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        [Default("")]        
        public InArgument<string> AttributeName { get; set; }

        [Input("Entity Name")]
        [Default("")]
        public InArgument<string> EntityName { get; set; }

        [RequiredArgument]
        [Input("Option Text")]
        [ReferenceTarget("")]
        public InArgument<string> OptionText { get; set; }

        [RequiredArgument]
        [Input("Option Value")]
        [ReferenceTarget("")]
        public InArgument<int> OptionValue { get; set; }

        [RequiredArgument]
        [Input("Language Code")]
        [ReferenceTarget("")]
        public InArgument<int> LanguageCode { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.InsertOptionValue(GlobalOptionSet.Get(executionContext), AttributeName.Get(executionContext), EntityName.Get(executionContext),
                OptionText.Get(executionContext), OptionValue.Get(executionContext), LanguageCode.Get(executionContext));
        }
    }
}
