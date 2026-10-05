using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class DeleteOptionValue : WorkflowActivityBase
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
        [Input("Option Value")]
        [ReferenceTarget("")]
        public InArgument<int> OptionValue { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var globalOptionSet = GlobalOptionSet.Get(executionContext);
            var attributeName = AttributeName.Get(executionContext);
            var entityName = EntityName.Get(executionContext);

            var optionValue = OptionValue.Get(executionContext);

            common.Trace($"attributeName={attributeName}--entityName={entityName}" );

            common.DeleteOptionValue(globalOptionSet,attributeName, entityName,  optionValue);
        }
    }
}
