using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        public ITracingService TracingService;
        public IWorkflowContext Context;
        public IOrganizationServiceFactory ServiceFactory;
        public IOrganizationService Service;

        /// <summary>
        /// Used by the workflow activities: pulls the tracing service, workflow context and organization service from the execution context.
        /// </summary>
        public Common(CodeActivityContext executionContext)
        {
            TracingService = executionContext.GetExtension<ITracingService>();
            Context = executionContext.GetExtension<IWorkflowContext>();
            ServiceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            Service = ServiceFactory.CreateOrganizationService(Context.UserId);
        }

        /// <summary>
        /// Used by unit tests and the console app, where no workflow execution context exists.
        /// </summary>
        public Common(IOrganizationService service, ITracingService tracingService = null, IWorkflowContext context = null)
        {
            Service = service;
            TracingService = tracingService ?? new NullTracingService();
            Context = context;
        }

        /// <summary>
        /// Writes a message to the trace log exactly as given, so it may contain { and } (interpolated
        /// values, JSON, FetchXML, stack traces).
        /// </summary>
        public void Trace(string message)
        {
            TracingService.Trace("{0}", message);
        }

        /// <summary>
        /// Writes a composite-format message (string.Format style) to the trace log.
        /// </summary>
        public void Trace(string format, params object[] args)
        {
            TracingService.Trace(format, args);
        }

        /// <summary>
        /// Tracing service that discards everything, so tracingService is never null.
        /// </summary>
        private sealed class NullTracingService : ITracingService
        {
            public void Trace(string format, params object[] args)
            {
            }
        }
    }
}