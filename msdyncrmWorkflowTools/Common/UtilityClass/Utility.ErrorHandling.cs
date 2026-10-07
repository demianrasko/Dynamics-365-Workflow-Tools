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
        /// The input's value, or an error naming the input when it's empty. [RequiredArgument] is only enforced in the
        /// designer; a dynamic value can still be empty when the workflow runs.
        /// </summary>
        /// <param name="value">The input's value.</param>
        /// <param name="inputName">The input's name as the designer shows it.</param>
        /// <exception cref="InvalidPluginExecutionException">The value is null or empty.</exception>
        public static string Required(string value, string inputName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidPluginExecutionException($"{inputName} is required.");
            }

            return value;
        }

        /// <summary>
        /// The input's value (e.g. a lookup), or an error naming the input when it's not set.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The value is null.</exception>
        public static T Required<T>(T value, string inputName) where T : class
        {
            return value ?? throw new InvalidPluginExecutionException($"{inputName} is required.");
        }

        /// <summary>
        /// An id typed as text (spaces and braces allowed), or an error naming the input when it's empty or not a GUID.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The text is empty or not a GUID.</exception>
        public static Guid RequiredGuid(string value, string inputName)
        {
            if (!Guid.TryParse(Required(value, inputName).Trim(), out var id))
            {
                throw new InvalidPluginExecutionException($"{inputName} '{value}' is not a valid GUID.");
            }

            return id;
        }

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
