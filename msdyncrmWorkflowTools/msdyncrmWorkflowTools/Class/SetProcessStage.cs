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


namespace msdyncrmWorkflowTools.Class
{
    public class SetProcessStage : CodeActivity
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> ClonningRecordURL { get; set; }

        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

        [Input("Process Stage Name")]
        public InArgument<string> ProcessStage { get; set; }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _ClonningRecordURL = this.ClonningRecordURL.Get(executionContext);
            if (_ClonningRecordURL == null || _ClonningRecordURL == "")
            {
                return;
            }
            string[] urlParts = _ClonningRecordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string objectTypeCode = urlParams[0].Replace("etc=", "");
            string entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            string objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            EntityReference process = this.Process.Get(executionContext);
            string processStage = this.ProcessStage.Get(executionContext);

            #endregion

            #region "SetProcessStage Execution"

            string stageName = processStage;

            Guid? stageId = null;
            if (processStage != null)
            {
                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Process stage: " + stageName);
                Entity stageReference = new Entity("processstage");

                QueryExpression queryStage = new QueryExpression("processstage");
                queryStage.ColumnSet = new ColumnSet();
                queryStage.Criteria.AddCondition(new ConditionExpression(
                        "stagename",
                        ConditionOperator.Equal,
                        stageName));

                queryStage.Criteria.AddCondition(new ConditionExpression(
                        "processid",
                        ConditionOperator.Equal,
                        process.Id));

                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Fetching the requested Stage.");
                try
                {
                    stageReference = objCommon.service.RetrieveMultiple(queryStage).Entities.FirstOrDefault();
                    if (stageReference == null)
                    {
                        throw new InvalidPluginExecutionException(
                            "Process stage " + stageName + " not found");
                    }

                    stageId = stageReference.Id;
                }
                catch (Exception e)
                {
                    objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Error trying to retrieve " +
                        "the requested stage. Exception: " + e.ToString());
                    throw new InvalidPluginExecutionException("An error occurred while trying to fetch process stage " +
                        stageName +
                    ". Exception message: " + e.Message + ". Inner Exception: " + e.ToString());
                }
            }

            //*************************
            RetrieveProcessInstancesRequest procOpp1Req = new RetrieveProcessInstancesRequest
            {
                EntityId = new Guid(objectId),
                EntityLogicalName = entityName
            };
            RetrieveProcessInstancesResponse procOpp1Resp = (RetrieveProcessInstancesResponse)objCommon.service.Execute(procOpp1Req);

          


            // Declare variables to store values returned in response
            Entity activeProcessInstance = null;
            Guid _processOpp1Id = Guid.Empty;
            string _procInstanceLogicalName = "";
            if (procOpp1Resp.Processes.Entities.Count > 0)
            {
                activeProcessInstance = procOpp1Resp.Processes.Entities[0]; // First record is the active process instance
                _processOpp1Id = activeProcessInstance.Id; // Id of the active process instance, which will be used
                                                           // later to retrieve the active path of the process instance

                Console.WriteLine("Current active process instance for the Opportunity record: '{0}'", activeProcessInstance["name"].ToString());

                // Get the BPF underlying entity logical name
                var uniqueProcessNameAttribute = "uniquename";
                var processEntity = objCommon.service.Retrieve("workflow", process.Id, new ColumnSet(uniqueProcessNameAttribute));
                _procInstanceLogicalName = processEntity.Attributes[uniqueProcessNameAttribute].ToString();

                //_procInstanceLogicalName = activeProcessInstance["name"].ToString().Replace(" ", string.Empty).ToLower(); // TO BE REMOVED: Incorrect as it only gets the display name.
            }
            else
            {
                Console.WriteLine("No process instances found for the opportunity record; aborting the sample.");
                Environment.Exit(1);
            }

            objCommon.tracingService.Trace("Starting the update");
           Entity processInstanceToUpdate= new Entity(_procInstanceLogicalName, _processOpp1Id);
            processInstanceToUpdate.Attributes.Add("activestageid", new EntityReference("processstage", stageId.Value));
            objCommon.tracingService.Trace("Starting the update2");
            objCommon.service.Update(processInstanceToUpdate);
            objCommon.tracingService.Trace("Starting the update3");
            //*************************
            /*
            // Set the active process and the phase if defined
            EntityReference objectReference = new EntityReference(entityName,new Guid(objectId));

            Entity entityToUpdate = new Entity();

            if (objectReference != null)
            {
                entityToUpdate.LogicalName = objectReference.LogicalName;
                entityToUpdate.Id = objectReference.Id;

                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Case tu update Id = " + objectReference.Id.ToString() + ", Name = " + objectReference.Name);
            }
            else
            {
                entityToUpdate.LogicalName = objCommon.context.PrimaryEntityName;
                entityToUpdate.Id = objCommon.context.PrimaryEntityId;

                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Case tu update Id = " + entityToUpdate.Id.ToString());
            }

            entityToUpdate["processid"] = process.Id;
            entityToUpdate["stageid"] = stageId.HasValue
                ? stageId.Value : default(Guid);

            try
            {
                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Updating " +
                    " Case to Update Id = " + entityToUpdate.Id +
                    " Process Id = " + process.Id + " | Process Name = " + process.Name +
                    " Stage Id = " + stageId.Value.ToString());

                objCommon.service.Update(entityToUpdate);

                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Case Id = " + entityToUpdate.Id.ToString() + " updated successfully.");
            }
            catch (Exception e)
            {
                objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] Error while setting " +
                    "the active BPF. Details: " + e.ToString());
                throw new InvalidPluginExecutionException("An error occurred while trying to update Business Process to Case Id = " + entityToUpdate.Id.ToString() +
                    "Exception message " + e.Message +
                    " Inner exception: " + e.ToString());
            }
            objCommon.tracingService.Trace("[Dynamics.ChangeBPFandPhase.Execute] End.");
            */
            #endregion

        }
    }
}
