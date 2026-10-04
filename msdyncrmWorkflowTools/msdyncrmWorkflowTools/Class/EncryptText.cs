using System.Activities;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class EncryptText : WorkflowActivityBase
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text to Encrypt")]
        [Default("")]
        public InArgument<string> TexttoEncrypt { get; set; }

      


        [Output("MD5 Hash Value")]
        public OutArgument<string> MD5HashValue { get; set; }

        [Output("SHA512 Hash Value")]
        public OutArgument<string> SHA512HashValue { get; set; }


        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {

            #region "Read Parameters"
            var _TexttoEncrypt = TexttoEncrypt.Get(executionContext);
           

            objCommon.Trace(string.Format("_TexttoEncrypt: {0} ",_TexttoEncrypt));
            #endregion


            #region "Encryption Execution"
            var _MD5HashValue = MD5Hash(_TexttoEncrypt);
            var _SHA512HashValue = SHA512Hash(_TexttoEncrypt);


            MD5HashValue.Set(executionContext, _MD5HashValue);
            SHA512HashValue.Set(executionContext, _SHA512HashValue);



            #endregion

        }

        public string SHA512Hash(string text)
        {
            
            byte[] result;
            SHA512 shaM = new SHA512Managed();
            shaM.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));
            result = shaM.Hash;


            var strBuilder = new StringBuilder();
            for (var i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits
                //for each byte
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

        public string MD5Hash(string text)
        {

            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it
            var result = md5.Hash;

            var strBuilder = new StringBuilder();
            for (var i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits
                //for each byte
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

    }
}
