using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class GetRecordID_Tests
    {
        CrmService objService = new CrmService();
        [TestMethod]
        public void GetRecordID1()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string objectID = classObj.GetRecordID("https://demianrasko.crm4.dynamics.com:443/main.aspx?etc=4207&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&histKey=885118818&newWindow=true&pagetype=entityrecord");
            Assert.AreEqual(objectID, "d3c3b3b2-ae19-e811-811f-5065f38a3a01");
        }
        [TestMethod]
        public void GetRecordID2()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string objectID = classObj.GetRecordID("");
            Assert.AreEqual(objectID, "");
        }
    }
}
