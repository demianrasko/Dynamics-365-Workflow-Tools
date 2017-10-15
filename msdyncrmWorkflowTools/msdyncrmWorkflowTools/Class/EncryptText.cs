using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace msdyncrmWorkflowTools
{
    public class EncryptText : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Text to Encrypt")]
        [Default("")]
        public InArgument<String> TexttoEncrypt { get; set; }

      


        [Output("MD5 Hash Value")]
        public OutArgument<String> MD5HashValue { get; set; }

        [Output("SHA512 Hash Value")]
        public OutArgument<String> SHA512HashValue { get; set; }


        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String _TexttoEncrypt = this.TexttoEncrypt.Get(executionContext);
           

            objCommon.tracingService.Trace(String.Format("_TexttoEncrypt: {0} ",_TexttoEncrypt));
            #endregion


            #region "Encryption Execution"
            string _MD5HashValue = MD5Hash(_TexttoEncrypt);
            string _SHA512HashValue = SHA512Hash(_TexttoEncrypt);


            this.MD5HashValue.Set(executionContext, _MD5HashValue);
            this.SHA512HashValue.Set(executionContext, _SHA512HashValue);



            #endregion

        }

        public string SHA512Hash(string text)
        {
            
            byte[] result;
            SHA512 shaM = new SHA512Managed();
            shaM.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));
            result = shaM.Hash;


            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
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
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits
                //for each byte
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

    }
}
