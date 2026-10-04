using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public class SetUserSettings : WorkflowActivityBase
    {
        /// <summary>The records-per-page values Dataverse accepts for paginglimit.</summary>
        private static readonly int[] ValidPagingLimits = { 25, 50, 75, 100, 250 };

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

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {

            #region "Read Parameters"

            var userReference = User.Get(executionContext);
            var pagingLimit = PagingLimit.Get(executionContext);
            var advancedFindStartupMode = AdvancedFindStartupMode.Get(executionContext);
            var timeZoneCode = TimeZoneCode.Get(executionContext);
            var helpLanguageId = HelpLanguageId.Get(executionContext);
            var uiLanguageId = UILanguageId.Get(executionContext);
            var defaultCalendarView = DefaultCalendarView.Get(executionContext);
            var isSendAsAllowed = IsSendAsAllowed.Get(executionContext);

            common.Trace($"UserID: {userReference.Id.ToString()} ");
            #endregion

            var newSettings = new Entity("usersettings");
            newSettings.Attributes.Add("systemuserid", userReference.Id);

            // TODO: Find a better way to do this
            if (pagingLimit != 0)
            {
                if (!ValidPagingLimits.Contains(pagingLimit))
                {
                    throw new InvalidPluginExecutionException(
                        $"PagingLimit must be 25, 50, 75, 100 or 250 (or 0 to leave it unchanged), not {pagingLimit}.");
                }

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

            // TODO: Investigate DefaultCalendarView and IsSendAsAllowed. Both are written on every run:
            // DefaultCalendarView defaults to 0 (Day), which passes the check below, and IsSendAsAllowed defaults
            // to false, so a step meant to change another setting also resets the user's calendar view and Send As.
            if (defaultCalendarView == 0 || defaultCalendarView == 1 || defaultCalendarView == 2)
            {
                newSettings.Attributes.Add("defaultcalendarview", defaultCalendarView);
            }

            newSettings.Attributes.Add("issendasallowed", isSendAsAllowed);

            common.service.Update(newSettings);
        }
    }
}
