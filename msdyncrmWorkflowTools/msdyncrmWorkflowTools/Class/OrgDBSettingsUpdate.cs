using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class OrgDBSettingsUpdate : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("orgDBSetting to Update")]
        [Default("")]
        public InArgument<string> orgDBSetting { get; set; }

        [RequiredArgument]
        [Input("Value")]
        [Default("")]
        public InArgument<string> Value { get; set; }

        

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var orgDbSetting = orgDBSetting.Get(executionContext).ToLower();
            var value = Value.Get(executionContext);
            #endregion

            #region "OrgDBSettings Update"
            objCommon.tracingService.Trace(
                $"{nameof(OrgDBSettingsUpdate)}.Execute - OrgDBSetting = {orgDbSetting}, New Value = {value}");

            var boolValue = false;

            try
            {
                var fetch =
                    $"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'><entity name='organization'><attribute name='{orgDbSetting}'/><order attribute='name' descending='false' /></entity></fetch>";

                objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - Fetch = " + fetch);

                var organizationColl = objCommon.service.RetrieveMultiple(new FetchExpression(fetch));

                if (organizationColl == null || organizationColl.Entities.Count <= 0)
                {
                    
                    return;
                }

                if (int.TryParse(value, out var numericValue))
                {
                    organizationColl.Entities[0].Attributes[orgDbSetting] = numericValue;
                }
                else if (bool.TryParse(value, out boolValue))
                {
                    organizationColl.Entities[0].Attributes[orgDbSetting] = boolValue;
                }
                else
                {
                    organizationColl.Entities[0].Attributes[orgDbSetting] = value;
                }

                objCommon.tracingService.Trace(
                    $"{nameof(OrgDBSettingsUpdate)}.Execute - Previous value orgDBSetting. NumericValue = {numericValue}, BoolValue = {boolValue}, StringValue = {value}");

                objCommon.service.Update(organizationColl.Entities[0]);

                objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute -  Update Ok");
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException($"[OrgDBSettingsUpdate] ERROR: {e}");
            }
            #endregion
        }
    }
}
