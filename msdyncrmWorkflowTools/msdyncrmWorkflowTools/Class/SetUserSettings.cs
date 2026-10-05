using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class SetUserSettings : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("User")]
        [ReferenceTarget(EntityNames.SystemUser)]
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

        //Specify the default calendar view: 0: day, 1: week, 2: month. -1 for ignore
        [RequiredArgument]
        [Input("DefaultCalendarView")]
        [Default("-1")]
        public InArgument<int> DefaultCalendarView { get; set; }

        [RequiredArgument]
        [Input("IsSendAsAllowed")]
        [Default("false")]
        public InArgument<bool> IsSendAsAllowed { get; set; }

        //Yes to ignore IsSendAsAllowed. Steps saved before this input existed read No, so they still write it.
        [Input("Leave IsSendAsAllowed Unchanged")]
        [Default("True")]
        public InArgument<bool> LeaveIsSendAsAllowedUnchanged { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.SetUserSettings(
                User.Get(executionContext).Id,
                PagingLimit.Get(executionContext),
                AdvancedFindStartupMode.Get(executionContext),
                TimeZoneCode.Get(executionContext),
                HelpLanguageId.Get(executionContext),
                UILanguageId.Get(executionContext),
                DefaultCalendarView.Get(executionContext),
                LeaveIsSendAsAllowedUnchanged.Get(executionContext) ? (bool?)null : IsSendAsAllowed.Get(executionContext));
        }
    }
}
