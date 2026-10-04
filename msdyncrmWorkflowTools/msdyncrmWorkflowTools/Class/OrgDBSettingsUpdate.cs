using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class OrgDBSettingsUpdate : WorkflowActivityBase
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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var orgDbSetting = orgDBSetting.Get(executionContext).ToLower();
            var value = Value.Get(executionContext);
            #endregion

            #region "OrgDBSettings Update"
            common.Trace(
                $"{nameof(OrgDBSettingsUpdate)}.Execute - OrgDBSetting = {orgDbSetting}, New Value = {value}");

            var boolValue = false;

            var organizationColl = common.Service.RetrieveMultiple(Queries.OrganizationSetting(orgDbSetting));

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

            common.Trace(
                $"{nameof(OrgDBSettingsUpdate)}.Execute - Previous value orgDBSetting. NumericValue = {numericValue}, BoolValue = {boolValue}, StringValue = {value}");

            common.Service.Update(organizationColl.Entities[0]);

            common.Trace("OrgDBSettingsUpdate.Execute -  Update Ok");
            #endregion
        }
    }
}
