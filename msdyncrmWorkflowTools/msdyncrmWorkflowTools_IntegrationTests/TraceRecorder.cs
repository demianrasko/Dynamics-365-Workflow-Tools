using Microsoft.Xrm.Sdk;
using System.Collections.Generic;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Tracing service that keeps the messages, so a test can write them to its output.
    /// </summary>
    internal sealed class TraceRecorder : ITracingService
    {
        public List<string> Messages { get; } = new List<string>();

        public void Trace(string format, params object[] args)
        {
            Messages.Add(args == null || args.Length == 0 ? format : string.Format(format, args));
        }
    }
}
