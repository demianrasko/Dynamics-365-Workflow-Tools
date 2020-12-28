using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class SetLookupFieldFromRecordUrl : CodeActivity
    {

        [RequiredArgument]
        [Input("Record URL")]
        public InArgument<string> RecordUrl { get; set; }

        [RequiredArgument]
        [Input("Lookup Field Name")]
        public InArgument<string> LookupFieldName { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"
            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            IWorkflowContext workflowContext = executionContext.GetExtension<IWorkflowContext>();
            #endregion

            #region "Read Parameters"
            string recordUrl = RecordUrl.Get(executionContext);
            string lookupFieldName = LookupFieldName.Get(executionContext);
            objCommon.tracingService.Trace("Inputs -- RecordUrl: " + recordUrl + " | LookupFieldName: " + lookupFieldName);
            #endregion "Read Parameters"

            #region "Set Lookup Value from Record URL"
            try
            {
                // Get the entity reference using the URL Parser
                DynamicUrlParser urlParser = new DynamicUrlParser(recordUrl);
                EntityReference entityRef = urlParser.ToEntityReference(objCommon.service);

                // Get details of the primary entity in the workflow context
                string primaryEntityName = workflowContext.PrimaryEntityName;
                Guid primaryEntityId = workflowContext.PrimaryEntityId;

                // Retrieve the current record with the lookup field
                ColumnSet columns = new ColumnSet(new string[] { lookupFieldName });
                Entity recordToUpdate = objCommon.service.Retrieve(primaryEntityName, primaryEntityId, columns);
                objCommon.tracingService.Trace("PrimaryEntityName: " + primaryEntityName + " | PrimaryEntityId: " + primaryEntityId);

                // Set the lookup field to our entity ref
                recordToUpdate[lookupFieldName] = entityRef;

                // Update the record
                objCommon.service.Update(recordToUpdate);
            }
            catch (Exception e)
            {

                throw new InvalidPluginExecutionException("Error updating lookup field. " + e.ToString());
            }
            #endregion "Set Lookup Value from Record URL"

        }
    }
}
