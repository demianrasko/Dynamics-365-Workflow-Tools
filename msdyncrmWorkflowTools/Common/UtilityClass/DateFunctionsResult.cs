using System;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// The outputs of the Date Functions activity (see <see cref="Utility.DateFunctions(DateTime, DateTime)"/>).
    /// </summary>
    public sealed class DateFunctionsResult
    {
        /// <summary>Date 1 minus Date 2.</summary>
        public TimeSpan Difference { get; set; }

        /// <summary>Date 1's day of the week, 0 for Sunday.</summary>
        public int DayOfWeek { get; set; }

        public int DayOfYear { get; set; }

        public int Day { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        /// <summary>Date 1's week of the year, by the current culture's rules.</summary>
        public int WeekOfYear { get; set; }
    }
}
