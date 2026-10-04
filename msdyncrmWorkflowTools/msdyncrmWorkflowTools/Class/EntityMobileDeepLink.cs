using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
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
    public class EntityMobileDeepLink : CodeActivity
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<String> RecordURL { get; set; }


        [Output("Mobile Deep Link Edit")]
        public OutArgument<string> MobileDeepLinkEdit { get; set; }

        [Output("Mobile Deep Link New")]
        public OutArgument<string> MobileDeepLinkNew { get; set; }

        [Output("Mobile Deep Link Default View")]
        public OutArgument<string> MobileDeepLinkDefaultView { get; set; }

        #endregion



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _recordURL = RecordURL.Get(executionContext);
            if (_recordURL == null || _recordURL == "")
            {
                return;
            }
            var urlParts = _recordURL.Split("?".ToArray());
            var urlParams = urlParts[1].Split("&".ToCharArray());
            var objectTypeCode = urlParams[0].Replace("etc=", "");
            var entityName = objCommon.sGetEntityNameFromCode(objectTypeCode, objCommon.service);
            var objectId = urlParams[1].Replace("id=", "");
            objCommon.tracingService.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);


            #endregion

            #region "Generating Mobile Deep Links Execution"

            var recordURLEdit = String.Format("ms-dynamicsxrm://?pagetype=entity&etn={0}&id={1}", entityName, objectId);
            var recordURLNew = String.Format("ms-dynamicsxrm://?pagetype=create&etn={0}", entityName);
            var recordURLDefaultView = String.Format("ms-dynamicsxrm://?pagetype=view&etn={0}", entityName);

            objCommon.tracingService.Trace("MobileDeepLinkEdit: "+ recordURLEdit);
            objCommon.tracingService.Trace("MobileDeepLinkNew: "+ recordURLNew);
            objCommon.tracingService.Trace("MobileDeepLinkDefaultView: "+ recordURLDefaultView);

            MobileDeepLinkEdit.Set(executionContext, recordURLEdit);
            MobileDeepLinkNew.Set(executionContext, recordURLNew);
            MobileDeepLinkDefaultView.Set(executionContext, recordURLDefaultView);

            objCommon.tracingService.Trace("returned object links OK");

            #endregion

        }

        
    }

}
