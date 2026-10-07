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
            var globalOptionSet = GlobalOptionSet.Get(executionContext);
            var attributeName = AttributeName.Get(executionContext);
            var entityName = EntityName.Get(executionContext);
            var optionText = OptionText.Get(executionContext);
            var optionValue = OptionValue.Get(executionContext);
            var languageCode = LanguageCode.Get(executionContext);

            common.Trace($"attributeName={attributeName}--entityName={entityName}--optionText={optionText}--languageCode={languageCode.ToString()}");

            common.InsertOptionValue(globalOptionSet,attributeName, entityName, optionText, optionValue, languageCode);
        }
    }
}
