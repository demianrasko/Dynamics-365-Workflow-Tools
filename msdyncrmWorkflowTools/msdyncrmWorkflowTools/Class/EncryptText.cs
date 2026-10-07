using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Encrypt Text")]
    public class EncryptText : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Text to Encrypt")]
        [Default("")]
        public InArgument<string> TexttoEncrypt { get; set; }

        [Output("MD5 Hash Value")]
        public OutArgument<string> MD5HashValue { get; set; }

        [Output("SHA512 Hash Value")]
        public OutArgument<string> SHA512HashValue { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var text = TexttoEncrypt.Get(executionContext);

            MD5HashValue.Set(executionContext, Utility.Md5Hash(text));
            SHA512HashValue.Set(executionContext, Utility.Sha512Hash(text));
        }
    }
}
