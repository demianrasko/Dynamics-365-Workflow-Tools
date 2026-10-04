using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class OrgDBSettingsRetrieve : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var orgDbSetting = orgDBSetting.Get(executionContext).ToLower();
            #endregion

            #region "OrgDBSettings Update"
            objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - OrgDBSetting = " + orgDbSetting );

            var boolValue = false;

            try
            {
                var fetch =
                    $"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'><entity name='organization'><attribute name='{orgDbSetting}'/><order attribute='name' descending='false' /></entity></fetch>";

                objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - Fetch = " + fetch);

                var organizationColl = objCommon.service.RetrieveMultiple(new FetchExpression(fetch));

                var stringValue = organizationColl.Entities[0].Attributes[orgDbSetting].ToString();

                if (int.TryParse(stringValue, out var numericValue))
                {
                    objCommon.tracingService.Trace("Numeric Value");
                }
                else if (bool.TryParse(stringValue, out boolValue))
                {
                    objCommon.tracingService.Trace("Bool Value");
                }
                else
                {
                    objCommon.tracingService.Trace("String Value");
                }

                StringValue.Set(executionContext, stringValue);
                NumericValue.Set(executionContext, numericValue);
                BoolValue.Set(executionContext, boolValue);

            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException($"[OrgDBSettingsUpdate] ERROR: {e}");
            }
            #endregion
        }
    }
}
