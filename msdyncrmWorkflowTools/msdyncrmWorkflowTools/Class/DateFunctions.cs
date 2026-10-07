using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Date Functions")]
    public class DateFunctions : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Date 1")]
        public InArgument<DateTime> Date1 { get; set; }

        [Input("Date 2")]
        public InArgument<DateTime> Date2 { get; set; }

        [Output("Total Days")]
        public OutArgument<double> TotalDays { get; set; }

        [Output("Total Hours")]
        public OutArgument<double> TotalHours { get; set; }

        [Output("Total Milliseconds")]
        public OutArgument<double> TotalMilliseconds { get; set; }

        [Output("Total Minutes")]
        public OutArgument<double> TotalMinutes { get; set; }

        [Output("Total Seconds")]
        public OutArgument<double> TotalSeconds { get; set; }

        [Output("Day Of Week")]
        public OutArgument<int> DayOfWeek { get; set; }

        [Output("Day Of Year")]
        public OutArgument<int> DayOfYear { get; set; }

        [Output("Day")]
        public OutArgument<int> Day { get; set; }

        [Output("Month")]
        public OutArgument<int> Month { get; set; }

        [Output("Year")]
        public OutArgument<int> Year { get; set; }

        [Output("Week Of Year")]
        public OutArgument<int> WeekOfYear { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var result = Utility.DateFunctions(Date1.Get(executionContext), Date2.Get(executionContext));

            TotalDays.Set(executionContext, result.Difference.TotalDays);
            TotalHours.Set(executionContext, result.Difference.TotalHours);
            TotalMilliseconds.Set(executionContext, result.Difference.TotalMilliseconds);
            TotalMinutes.Set(executionContext, result.Difference.TotalMinutes);
            TotalSeconds.Set(executionContext, result.Difference.TotalSeconds);
            DayOfWeek.Set(executionContext, result.DayOfWeek);
            DayOfYear.Set(executionContext, result.DayOfYear);
            Day.Set(executionContext, result.Day);
            Month.Set(executionContext, result.Month);
            Year.Set(executionContext, result.Year);
            WeekOfYear.Set(executionContext, result.WeekOfYear);
        }
    }
}
