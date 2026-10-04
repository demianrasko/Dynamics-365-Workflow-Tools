using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class TranslateText : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text To Translate")]
        [Default("")]
        public InArgument<String> TextToTranslate { get; set; }

        [RequiredArgument]
        [Input("Language")]
        [Default("")]
        public InArgument<string> Language { get; set; }

        [RequiredArgument]
        [Input("Authentication key")]
        [Default("")]
        public InArgument<string> Authenticationkey { get; set; }


        [Output("Translated Text")]
        public OutArgument<String> TranslatedText { get; set; }
        
        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var _TextToTranslate = TextToTranslate.Get(executionContext);
            var _Language = Language.Get(executionContext);
            var _Authenticationkey = Authenticationkey.Get(executionContext);

            #endregion


            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            var res=commonClass.TranslateText(_TextToTranslate, _Language, _Authenticationkey);

            if (res == null) res = "";

            TranslatedText.Set(executionContext, res);
            
        }
    }
}
