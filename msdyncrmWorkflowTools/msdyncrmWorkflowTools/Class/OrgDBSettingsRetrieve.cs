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
        public InArgument<String> orgDBSetting { get; set; }


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
            var _orgDBSetting = orgDBSetting.Get(executionContext).ToLower();
            #endregion

            #region "OrgDBSettings Update"
            objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - OrgDBSetting = " + _orgDBSetting );

            var _NumericValue = 0;
            var _BoolValue = false;
            var _StringValue = "";

            try
            {
                var fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                                 "   <entity name='organization'>" +
                                 "         <attribute name='" + _orgDBSetting + "' />" +
                                 "                   <order attribute='name' descending='false' />" +
                                 "   </entity>" +
                                 "</fetch>";

                objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - Fetch = " + fetch);

                var organizationColl = objCommon.service.RetrieveMultiple(new FetchExpression(fetch));

                _StringValue=organizationColl.Entities[0].Attributes[_orgDBSetting].ToString();
                if (int.TryParse(_StringValue, out _NumericValue))
                    objCommon.tracingService.Trace("Numeric Value");
                else if (bool.TryParse(_StringValue, out _BoolValue))
                    objCommon.tracingService.Trace("Bool Value");
                else
                    objCommon.tracingService.Trace("String Value");

                StringValue.Set(executionContext, _StringValue);
                NumericValue.Set(executionContext, _NumericValue);
                BoolValue.Set(executionContext, _BoolValue);

            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException("[OrgDBSettingsUpdate] ERROR: " + e.ToString());
            }
            #endregion
        }
        

    }
}
