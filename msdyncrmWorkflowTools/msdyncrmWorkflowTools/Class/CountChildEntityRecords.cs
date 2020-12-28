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

        [Input("FetchXML Filter (Child)")]
        [ReferenceTarget("")]
        public InArgument<String> FilterExpressionXml { get; set; }


        [Output("Result")]
        public OutArgument<int> Result { get; set; }
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _childEntityName = this.ChildEntityName.Get(executionContext);
            String _parentLookupName = this.ParentLookupName.Get(executionContext);
            String _recordURL = this.RecordURL.Get(executionContext);
            String _filterExpressionXml = this.FilterExpressionXml.Get(executionContext);
            objCommon.tracingService.Trace("ChildEntityName=" + _childEntityName + "--ParentLookupName=" + _parentLookupName + "--RecordURL=" + _recordURL + "--FilterExpressionXml=" + _filterExpressionXml);
            if (_recordURL == null || _recordURL == "")
            {
                return;
            }
            string[] urlParts = _recordURL.Split("?".ToArray());
            string[] urlParams = urlParts[1].Split("&".ToCharArray());
            string ParentObjectTypeCode = urlParams[0].Replace("etc=", "");
            string ParenEntityName = objCommon.sGetEntityNameFromCode(ParentObjectTypeCode, objCommon.service);
            string ParentEntityId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ParentObjectTypeCode=" + ParentObjectTypeCode + "--ParentId=" + ParentEntityId);
            #endregion


            #region "Process"

            try
            {
                var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                    <entity name='{0}'>
                                    <filter type='and'>
                                        <condition attribute='{1}' operator='eq' value='{2}' />
                                        {3}
                                        </filter>
                                    </entity>
                                </fetch>";
                fetchXml = string.Format(fetchXml, _childEntityName, _parentLookupName, ParentEntityId, _filterExpressionXml);
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
