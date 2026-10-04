using System.Activities;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var _GlobalOptionSet = GlobalOptionSet.Get(executionContext);
            var _AttributeName = AttributeName.Get(executionContext);
            var _EntityName = EntityName.Get(executionContext);
            var _OptionText = OptionText.Get(executionContext);
            var _OptionValue = OptionValue.Get(executionContext);
            var _LanguageCode = LanguageCode.Get(executionContext);

            objCommon.Trace("_AttributeName=" + _AttributeName + "--_EntityName=" + _EntityName+ "--_OptionText="+ _OptionText+ "--_LanguageCode="+ _LanguageCode.ToString());
            #endregion


            #region "Insert Option Value"

            try
            {
                objCommon.InsertOptionValue(_GlobalOptionSet,_AttributeName, _EntityName, _OptionText, _OptionValue, _LanguageCode);

                
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                objCommon.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            catch (System.Exception ex)
            {
                objCommon.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
            }
            #endregion

        }


    }
}
