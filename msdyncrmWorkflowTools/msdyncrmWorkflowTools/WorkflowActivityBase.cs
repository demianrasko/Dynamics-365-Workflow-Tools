using Microsoft.Xrm.Sdk;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// Base class for every custom workflow activity: creates <see cref="Common"/>, runs the activity,
    /// and handles exceptions the same way everywhere.
    /// </summary>
    /// <remarks>
    /// Activities override <see cref="ExecuteActivity"/> instead of Execute and don't need their own
    /// try/catch. Any exception is written to the trace log in full (see <see cref="Utility.HandleExceptions"/>)
    /// and reaches the user as an <see cref="InvalidPluginExecutionException"/>. Throw
    /// InvalidPluginExecutionException yourself for messages the user should see exactly as written.
    /// </remarks>
    public abstract class WorkflowActivityBase : CodeActivity
    {
        protected sealed override void Execute(CodeActivityContext executionContext)
        {
            var common = new Common(executionContext);
            common.Trace("{0} started", GetType().Name);

            try
            {
                ExecuteActivity(executionContext, common);
                common.Trace("{0} finished", GetType().Name);
            }
            catch (InvalidPluginExecutionException ex)
            {
                // Messages raised on purpose (validation etc.): show them to the user as-is.
                common.Trace(Utility.HandleExceptions(ex));
                throw;
            }
            catch (Exception ex)
            {
                // Anything else: full details to the trace log, readable message to the user.
                common.Trace(Utility.HandleExceptions(ex));
                throw new InvalidPluginExecutionException($"{GetType().Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// The activity's work. Throw <see cref="InvalidPluginExecutionException"/> for messages the user should see.
        /// </summary>
        /// <param name="executionContext">The workflow execution context, for reading and writing arguments.</param>
        /// <param name="common">Organization service, tracing, workflow context and the shared helpers.</param>
        protected abstract void ExecuteActivity(CodeActivityContext executionContext, Common common);
    }
}
