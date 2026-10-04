using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using msdyncrmWorkflowTools;
using System.ServiceModel;

namespace msdyncrmWorkflowTools
{


    public class CountChildEntityRecords : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Child Entity Schema Name")]
        [Default("")]
        public InArgument<String> ChildEntityName { get; set; }

        [RequiredArgument]
        [Input("Parent Lookup Field Name on Child")]
        [Default("")]
        public InArgument<String> ParentLookupName { get; set; }

        [RequiredArgument]
        [Input("Record URL (Parent)")]
        [ReferenceTarget("")]
        public InArgument<String> RecordURL { get; set; }


        [Output("Result")]
        public OutArgument<int> Result { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _childEntityName = this.ChildEntityName.Get(executionContext);
            var _parentLookupName = this.ParentLookupName.Get(executionContext);
            var _recordURL = this.RecordURL.Get(executionContext);
            objCommon.tracingService.Trace("ChildEntityName=" + _childEntityName + "--ParentLookupName=" + _parentLookupName + "--RecordURL=" + _recordURL);
            if (_recordURL == null || _recordURL == "")
            {
                return;
            }
            var urlParts = _recordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            var ParenEntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            var ParentEntityId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentEntityId);
            #endregion


            #region "Process"

            try
            {
                var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                    <entity name='{0}'>                                                                           
                                    <filter type='and'>
                                        <condition attribute='{1}' operator='eq' value='{2}' />                                                                                 
                                        </filter>
                                    </entity>
                                </fetch>";
                fetchXml = string.Format(fetchXml, _childEntityName, _parentLookupName, ParentEntityId);
                objCommon.tracingService.Trace(String.Format("FetchXML: {0} ", fetchXml));
                var results = objCommon.service.RetrieveMultiple(new FetchExpression(fetchXml));

                this.Result.Set(executionContext, results.Entities.Count);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw ex;
            }
            #endregion
        }
    }
}
