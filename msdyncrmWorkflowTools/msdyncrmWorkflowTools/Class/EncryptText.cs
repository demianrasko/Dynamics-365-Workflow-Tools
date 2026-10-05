using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class EncryptText : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text to Encrypt")]
        [Default("")]
        public InArgument<string> TexttoEncrypt { get; set; }

        [Output("MD5 Hash Value")]
        public OutArgument<string> Md5HashValue { get; set; }

        [Output("SHA512 Hash Value")]
        public OutArgument<string> Sha512HashValue { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TexttoEncrypt.Get(executionContext);

            common.Trace($"Text: {text} ");

            var md5HashValue = Utility.Md5Hash(text);
            var sha512HashValue = Utility.Sha512Hash(text);

            Md5HashValue.Set(executionContext, md5HashValue);
            Sha512HashValue.Set(executionContext, sha512HashValue);
        }
    }
}
