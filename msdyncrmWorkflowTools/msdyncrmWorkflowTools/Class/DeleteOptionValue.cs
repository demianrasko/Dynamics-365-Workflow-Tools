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

   
    public class DeleteOptionValue : CodeActivity
    {

        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Global Option Set")]
        [Default("false")]
        public InArgument<bool> GlobalOptionSet { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        [Default("")]        
        public InArgument<String> AttributeName { get; set; }

        [Input("Entity Name")]
        [Default("")]
        public InArgument<String> EntityName { get; set; }

       
        [RequiredArgument]
        [Input("Option Value")]
        [ReferenceTarget("")]
        public InArgument<int> OptionValue { get; set; }

       
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            bool _GlobalOptionSet = this.GlobalOptionSet.Get(executionContext);
            String _AttributeName = this.AttributeName.Get(executionContext);
            String _EntityName = this.EntityName.Get(executionContext);
            
            int _OptionValue = this.OptionValue.Get(executionContext);
            
            objCommon.tracingService.Trace("_AttributeName=" + _AttributeName + "--_EntityName=" + _EntityName );
            #endregion


            #region "Insert Option Value"

            try
            {
                msdyncrmWorkflowTools_Class commonClass = new msdyncrmWorkflowTools_Class(objCommon.service);
                commonClass.DeleteOptionValue(_GlobalOptionSet,_AttributeName, _EntityName,  _OptionValue);

                
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            catch (System.Exception ex)
            {
                objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //throw ex;
            }
            #endregion

        }


    }
}
