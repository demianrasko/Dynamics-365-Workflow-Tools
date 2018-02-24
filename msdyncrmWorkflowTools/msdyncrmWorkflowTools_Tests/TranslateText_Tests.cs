using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class TranslateText_Tests
    {
        CrmService objService = new CrmService();

        [TestMethod]
        public void TranslateText1()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string traslated = classObj.TranslateText("Hola", "pt", "a60244d696fc421f85e2adcd386bdf9e");

            Assert.AreEqual(traslated, "Olá");
        }

        [TestMethod]
        public void TranslateText2()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string traslated = classObj.TranslateText("Hola", "en", "a60244d696fc421f85e2adcd386bdf9e");

            Assert.AreEqual(traslated, "Hello");
        }

        [TestMethod]
        public void TranslateText3()
        {
            var classObj = new msdyncrmWorkflowTools_Class(objService.service);
            string traslated = classObj.TranslateText("Hello", "es", "a60244d696fc421f85e2adcd386bdf9e");

            Assert.AreEqual(traslated, "Hola");
        }
    }
}
