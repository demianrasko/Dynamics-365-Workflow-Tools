using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class Utility_Tests
    {
        private const string RecordUrl = "https://demianrasko.crm4.dynamics.com:443/main.aspx?etc=4207&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&histKey=885118818&newWindow=true&pagetype=entityrecord";

        [TestMethod]
        public void ParseRecordUrl_ReturnsTypeCodeAndId()
        {
            var parsedUrl = Utility.ParseRecordUrl(RecordUrl);

            Assert.AreEqual("4207", parsedUrl.ObjectTypeCode);
            Assert.AreEqual("d3c3b3b2-ae19-e811-811f-5065f38a3a01", parsedUrl.Id);
        }

        [TestMethod]
        public void ParseRecordUrl_FindsParametersInAnyOrder()
        {
            var parsedUrl = Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx?pagetype=entityrecord&id=d3c3b3b2-ae19-e811-811f-5065f38a3a01&etc=1");

            Assert.AreEqual("1", parsedUrl.ObjectTypeCode);
            Assert.AreEqual("d3c3b3b2-ae19-e811-811f-5065f38a3a01", parsedUrl.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseRecordUrl_ThrowsWhenThereIsNoQueryString()
        {
            Utility.ParseRecordUrl("https://org.crm.dynamics.com/main.aspx");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ParseRecordUrl_ThrowsWhenEmpty()
        {
            Utility.ParseRecordUrl(string.Empty);
        }

        [TestMethod]
        public void GetMask_NoFlags_ReturnsNone()
        {
            Assert.AreEqual(AccessRights.None, Utility.GetMask(false, false, false, false, false, false, false));
        }

        [TestMethod]
        public void GetMask_CombinesSelectedFlags()
        {
            var mask = Utility.GetMask(read: true, write: true, append: false, appendTo: false, delete: false, share: true, assign: false);

            Assert.AreEqual(AccessRights.ReadAccess | AccessRights.WriteAccess | AccessRights.ShareAccess, mask);
        }

        [TestMethod]
        public void GetMask_AllFlags()
        {
            var mask = Utility.GetMask(true, true, true, true, true, true, true);

            Assert.AreEqual(
                AccessRights.ReadAccess | AccessRights.WriteAccess | AccessRights.AppendAccess | AccessRights.AppendToAccess |
                AccessRights.DeleteAccess | AccessRights.ShareAccess | AccessRights.AssignAccess,
                mask);
        }
    }
}
