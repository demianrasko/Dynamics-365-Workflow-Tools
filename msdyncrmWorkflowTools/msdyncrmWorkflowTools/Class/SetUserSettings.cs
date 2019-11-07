using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class SetUserSettings : CodeActivity
    {


        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [RequiredArgument]
        [Input("PagingLimit")]
        [Default("0")]
        public InArgument<int> PagingLimit { get; set; }
        //Specify how many records per view. Value can be 25,50,75,100,250  

        [RequiredArgument]
        [Input("AdvancedFindStartupMode")]
        [Default("1")]
        public InArgument<int> AdvancedFindStartupMode { get; set; }
        //Specify AdvancedFind mode. 1:simple, 2:detail.

        [RequiredArgument]
        [Input("TimeZoneCode")]
        [Default("0")]
        public InArgument<int> TimeZoneCode { get; set; }
        //Specify TimeZoneCode for users. Use Get-CrmTimeZones to see all options 0 for ignore.

        [RequiredArgument]
        [Input("HelpLanguageId")]
        [Default("0")]
        public InArgument<int> HelpLanguageId { get; set; }
        //Specify Unique identifier of the Help language. 0 for ignore

        [RequiredArgument]
        [Input("UILanguageId")]
        [Default("0")]
        public InArgument<int> UILanguageId { get; set; }
        //Specify Unique identifier of the language in which to view the user interface (UI). 0 for ignore


        [RequiredArgument]
        [Input("DefaultCalendarView")]
        [Default("0")]
        public InArgument<int> DefaultCalendarView { get; set; }
 //specify the default calendar view values: Day
        /*
0: Show the day by default.
2: Show the month by default.
1: Show the week by default
            */


        [RequiredArgument]
        [Input("IsSendAsAllowed")]
        [Default("false")]
        public InArgument<bool> IsSendAsAllowed { get; set; }
        
       


        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"

            EntityReference userReference = this.User.Get(executionContext);
            int pagingLimit = this.PagingLimit.Get(executionContext);
            int advancedFindStartupMode = this.AdvancedFindStartupMode.Get(executionContext);
            int timeZoneCode = this.TimeZoneCode.Get(executionContext);
            int helpLanguageId = this.HelpLanguageId.Get(executionContext);
            int uiLanguageId = this.UILanguageId.Get(executionContext);
            int defaultCalendarView = this.DefaultCalendarView.Get(executionContext);
            bool isSendAsAllowed = this.IsSendAsAllowed.Get(executionContext);
            

            objCommon.tracingService.Trace(String.Format("UserID: {0} ", userReference.Id.ToString()));
            #endregion

            Entity newSettings = new Entity("usersettings");
            newSettings.Attributes.Add("systemuserid", userReference.Id);
            if (pagingLimit != 0)
            {
                newSettings.Attributes.Add("paginglimit", pagingLimit);
            }
            if (advancedFindStartupMode == 1 || advancedFindStartupMode == 2)
            {
                newSettings.Attributes.Add("advancedfindstartupmode", advancedFindStartupMode);
            }
            if (timeZoneCode != 0)
            {
                newSettings.Attributes.Add("timezonecode", timeZoneCode);
            }
            if (helpLanguageId != 0)
            {
                newSettings.Attributes.Add("helplanguageid", helpLanguageId);
            }
            if (uiLanguageId != 0)
            {
                newSettings.Attributes.Add("uilanguageid", uiLanguageId);
            }
            if (defaultCalendarView == 0 || defaultCalendarView == 1 || defaultCalendarView == 2)
            {
                newSettings.Attributes.Add("defaultcalendarview", defaultCalendarView);
            }
            newSettings.Attributes.Add("issendasallowed", isSendAsAllowed);
            

            objCommon.service.Update(newSettings);


        }
    }
}
