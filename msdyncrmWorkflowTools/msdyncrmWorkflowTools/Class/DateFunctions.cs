using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class DateFunctions : WorkflowActivityBase
    {
        #region "Parameter Definition"

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

        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var date1 = Date1.Get(executionContext);
            var date2 = Date2.Get(executionContext);

            #endregion

            var difference = TimeSpan.Zero;

            var dayOfWeek = 0;
            var dayOfYear = 0;
            var day = 0;
            var month = 0;
            var year = 0;
            var weekOfYear = 0;

            Utility.DateFunctions(date1, date2, ref difference,
                ref dayOfWeek, ref dayOfYear, ref day, ref month, ref year, ref weekOfYear);

            TotalDays.Set(executionContext, difference.TotalDays);
            TotalHours.Set(executionContext, difference.TotalHours);
            TotalMilliseconds.Set(executionContext, difference.TotalMilliseconds);
            TotalMinutes.Set(executionContext, difference.TotalMinutes);
            TotalSeconds.Set(executionContext, difference.TotalSeconds);
            DayOfWeek.Set(executionContext, dayOfWeek);
            DayOfYear.Set(executionContext, dayOfYear);
            Day.Set(executionContext, day);
            Month.Set(executionContext, month);
            Year.Set(executionContext, year);
            WeekOfYear.Set(executionContext, weekOfYear);
        }
    }
}
