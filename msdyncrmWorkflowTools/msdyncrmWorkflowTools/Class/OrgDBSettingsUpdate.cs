using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class OrgDBSettingsUpdate : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("orgDBSetting to Update")]
        [Default("")]
        public InArgument<String> orgDBSetting { get; set; }

        [RequiredArgument]
        [Input("Value")]
        [Default("")]
        public InArgument<String> Value { get; set; }

        

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _orgDBSetting = this.orgDBSetting.Get(executionContext).ToLower();
            String _Value = this.Value.Get(executionContext);
            #endregion

            #region "OrgDBSettings Update"
            objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - OrgDBSetting = " + _orgDBSetting + ", New Value = " + _Value);

            int NumericValue = 0;
            bool BoolValue = false;
            string StringValue = _orgDBSetting;

            try
            {
                string fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                                 "   <entity name='organization'>" +
                                 "         <attribute name='" + _orgDBSetting + "' />" +
                                 "                   <order attribute='name' descending='false' />" +
                                 "   </entity>" +
                                 "</fetch>";

                objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - Fetch = " + fetch);

                EntityCollection organizationColl = objCommon.service.RetrieveMultiple(new FetchExpression(fetch));

                if (organizationColl != null && organizationColl.Entities.Count > 0)
                {
                    if (int.TryParse(_Value, out NumericValue))
                        organizationColl.Entities[0].Attributes[_orgDBSetting] = NumericValue;
                    else if (bool.TryParse(_orgDBSetting, out BoolValue))
                        organizationColl.Entities[0].Attributes[_orgDBSetting] = BoolValue;
                    else
                        organizationColl.Entities[0].Attributes[_orgDBSetting] = StringValue;

                    objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute - Previous value orgDBSetting. NumericValue = " + NumericValue.ToString() + ", BoolValue = " + BoolValue.ToString() + ", StringValue = " + StringValue);

                    objCommon.service.Update(organizationColl.Entities[0]);

                    objCommon.tracingService.Trace("OrgDBSettingsUpdate.Execute -  Update Ok");
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException("[OrgDBSettingsUpdate] ERROR: " + e.ToString());
            }
            #endregion
        }
        

    }
}
