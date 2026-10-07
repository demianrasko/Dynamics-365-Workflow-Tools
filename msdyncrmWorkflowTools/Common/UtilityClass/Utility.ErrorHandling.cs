using Microsoft.Xrm.Sdk;
using System;
using System.Net;
using System.ServiceModel;
using System.Text;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        /// <summary>
        /// Formats an exception as text for the tracing service or an error message: type, message,
        /// Dataverse fault details (error code, timestamp, activity id, trace text, nested inner faults),
        /// stack trace, and the same for every inner exception.
        /// </summary>
        /// <remarks>
        /// The result can contain { and } (stack traces, JSON, FetchXML), so trace it as
        /// <c>tracingService.Trace("{0}", Utility.HandleExceptions(ex))</c> rather than passing it as the format string.
        /// </remarks>
        /// <param name="ex">The exception to format. Null returns an empty string.</param>
        /// <returns>The formatted exception details.</returns>
        public static string HandleExceptions(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current != ex)
                {
                    sb.AppendLine("--- Inner exception ---");
                }

                AppendException(current, sb);
            }

            return sb.ToString();
        }

        private static void AppendException(Exception ex, StringBuilder sb)
        {
            sb.AppendLine($"Type:\t{ex.GetType().FullName}");
            sb.AppendLine($"Message:\t{ex.Message}");

            switch (ex)
            {
                case FaultException<OrganizationServiceFault> organizationFault:
                    for (var fault = organizationFault.Detail; fault != null; fault = fault.InnerFault)
                    {
                        if (fault != organizationFault.Detail)
                        {
                            sb.AppendLine("--- Inner fault ---");
                        }

                        AppendFault(fault, sb);

                        if (!string.IsNullOrEmpty(fault.TraceText))
                        {
                            sb.AppendLine($"Trace:\t{fault.TraceText}");
                        }
                    }

                    break;

                case FaultException<DiscoveryServiceFault> discoveryFault:
                    for (var fault = discoveryFault.Detail; fault != null; fault = fault.InnerFault)
                    {
                        if (fault != discoveryFault.Detail)
                        {
                            sb.AppendLine("--- Inner fault ---");
                        }

                        AppendFault(fault, sb);
                    }

                    break;

                case WebException webException:
                    sb.AppendLine($"Status:\t{webException.Status}");
                    break;
            }

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine($"Stack Trace:\t{ex.StackTrace}");
            }
        }

        private static void AppendFault(BaseServiceFault fault, StringBuilder sb)
        {
            sb.AppendLine($"Code:\t{fault.ErrorCode}");
            sb.AppendLine($"Fault Message:\t{fault.Message}");
            sb.AppendLine($"Timestamp:\t{fault.Timestamp}");
            sb.AppendLine($"Activity Id:\t{fault.ActivityId}");
        }
    }
}
