using System.Activities;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class OrgDBSettingsRetrieve : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("orgDBSetting to Update")]
        [Default("")]
        public InArgument<string> orgDBSetting { get; set; }


        [Output("String Value")]
        public OutArgument<string> StringValue { get; set; }

        [Output("Numeric Value")]
        public OutArgument<decimal> NumericValue { get; set; }

        [Output("Bool Value")]
        public OutArgument<bool> BoolValue { get; set; }


        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var orgDbSetting = orgDBSetting.Get(executionContext).ToLower();
            #endregion

            #region "OrgDBSettings Update"
            objCommon.Trace("OrgDBSettingsUpdate.Execute - OrgDBSetting = " + orgDbSetting );

            var boolValue = false;

            var fetch =
                $"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'><entity name='organization'><attribute name='{orgDbSetting}'/><order attribute='name' descending='false' /></entity></fetch>";

            objCommon.Trace("OrgDBSettingsUpdate.Execute - Fetch = " + fetch);

            var organizationColl = objCommon.service.RetrieveMultiple(new FetchExpression(fetch));

            var stringValue = organizationColl.Entities[0].Attributes[orgDbSetting].ToString();

            if (int.TryParse(stringValue, out var numericValue))
            {
                objCommon.Trace("Numeric Value");
            }
            else if (bool.TryParse(stringValue, out boolValue))
            {
                objCommon.Trace("Bool Value");
            }
            else
            {
                objCommon.Trace("String Value");
            }

            StringValue.Set(executionContext, stringValue);
            NumericValue.Set(executionContext, numericValue);
            BoolValue.Set(executionContext, boolValue);

            #endregion
        }
    }
}
