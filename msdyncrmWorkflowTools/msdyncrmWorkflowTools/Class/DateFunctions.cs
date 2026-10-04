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
    public class DateFunctions : CodeActivity
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

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var date1 = Date1.Get(executionContext);
            var date2 = Date2.Get(executionContext);
            
            #endregion

           

            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            var difference = new TimeSpan();
            var DayOfWeek = 0;
            var DayOfYear = 0;
            var Day = 0;
            var Month = 0;
            var Year = 0;
            var WeekOfYear = 0;
            commonClass.DateFunctions(date1, date2, ref difference,
                ref DayOfWeek, ref DayOfYear, ref Day, ref Month, ref Year, ref WeekOfYear);

            
            TotalDays.Set(executionContext, difference.TotalDays);
            TotalHours.Set(executionContext, difference.TotalHours);
            TotalMilliseconds.Set(executionContext, difference.TotalMilliseconds);
            TotalMinutes.Set(executionContext, difference.TotalMinutes);
            TotalSeconds.Set(executionContext, difference.TotalSeconds);


            this.DayOfWeek.Set(executionContext, DayOfWeek);
            this.DayOfYear.Set(executionContext, DayOfYear);
            this.Day.Set(executionContext, Day);
            this.Month.Set(executionContext, Month);
            this.Year.Set(executionContext, Year);
            this.WeekOfYear.Set(executionContext, WeekOfYear);

            

        }


    }
}
