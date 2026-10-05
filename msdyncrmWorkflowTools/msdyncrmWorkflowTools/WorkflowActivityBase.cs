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
            common.Trace($"{GetType().Name} started");

            try
            {
                ExecuteActivity(executionContext, common);
                common.Trace($"{GetType().Name} finished");
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

        /// <summary>
        /// Runs a call whose errors are reported through the activity's "Failed" and "Failure Message" outputs
        /// instead of failing the workflow (the AI activities work this way), and sets the result output.
        /// </summary>
        protected static void SetResultOrFailure(CodeActivityContext executionContext, Common common, Func<string> call,
            OutArgument<string> result, OutArgument<bool> failed, OutArgument<string> failureMessage)
        {
            try
            {
                result.Set(executionContext, call() ?? string.Empty);
                failed.Set(executionContext, false);
                failureMessage.Set(executionContext, string.Empty);
            }
            catch (Exception ex)
            {
                common.Trace(Utility.HandleExceptions(ex));
                result.Set(executionContext, string.Empty);
                failed.Set(executionContext, true);
                failureMessage.Set(executionContext, ex.Message);
            }
        }
    }
}
