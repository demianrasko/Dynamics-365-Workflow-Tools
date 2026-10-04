using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class InsertOptionValue : WorkflowActivityBase
    {
        #region "Parameter Definition"

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
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var _GlobalOptionSet = GlobalOptionSet.Get(executionContext);
            var _AttributeName = AttributeName.Get(executionContext);
            var _EntityName = EntityName.Get(executionContext);
            var _OptionText = OptionText.Get(executionContext);
            var _OptionValue = OptionValue.Get(executionContext);
            var _LanguageCode = LanguageCode.Get(executionContext);

            common.Trace("_AttributeName=" + _AttributeName + "--_EntityName=" + _EntityName+ "--_OptionText="+ _OptionText+ "--_LanguageCode="+ _LanguageCode.ToString());
            #endregion

            #region "Insert Option Value"

            common.InsertOptionValue(_GlobalOptionSet,_AttributeName, _EntityName, _OptionText, _OptionValue, _LanguageCode);

            #endregion
        }
    }
}
