using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotSee.VirtualNodes;

namespace VirtualNodesTest
{
    /// <summary>
    /// Some silly tests, nothing much, really.
    /// </summary>
    [TestClass]
    public class HelperTests
    {
        [TestMethod]
        public void MatchDuplicateNameTestHappyPath()
        {

            string potentialDuplicateName;
            string nodeName;

            bool result;

            potentialDuplicateName = "aaa (3)";
            nodeName = "aaa";

            result = Helpers.MatchDuplicateName(potentialDuplicateName, nodeName);
            Assert.IsTrue(result);

            potentialDuplicateName = "aaa1";
            nodeName = "aaa";

            result = Helpers.MatchDuplicateName(potentialDuplicateName, nodeName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchDuplicateNameTestFail()
        {

            string potentialDuplicateName;
            string nodeName;

            bool result;

            potentialDuplicateName = "aaa1";
            nodeName = "aaa";

            result = Helpers.MatchDuplicateName(potentialDuplicateName, nodeName);
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void MatchDuplicateNameTestFail2()
        {

            string potentialDuplicateName;
            string nodeName;

            bool result;

            potentialDuplicateName = "aaa (1) (1)";
            nodeName = "aaa";

            result = Helpers.MatchDuplicateName(potentialDuplicateName, nodeName);
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void GetMaxNodeNameNumberingTest()
        {

            string potentialDuplicateName;
            string nodeName;

            int result;

            potentialDuplicateName = "aaa (1)";
            nodeName = "aaa";
            int maxNumber = 0;

            result = Helpers.GetMaxNodeNameNumbering(potentialDuplicateName, nodeName, maxNumber);
            Assert.AreEqual(result, 1);
        }

        [TestMethod]
        public void GetMaxNodeNameNumberingTest2()
        {

            string potentialDuplicateName;
            string nodeName;

            int result;

            potentialDuplicateName = "aaa (4)";
            nodeName = "aaa";
            int maxNumber = 0;

            result = Helpers.GetMaxNodeNameNumbering(potentialDuplicateName, nodeName, maxNumber);
            Assert.AreEqual(result, 4);
        }

        [TestMethod]
        public void GetMaxNodeNameNumberingTest3()
        {

            string potentialDuplicateName;
            string nodeName;

            int result;

            potentialDuplicateName = "aaa (5)";
            nodeName = "aaa";
            int maxNumber = 6;

            result = Helpers.GetMaxNodeNameNumbering(potentialDuplicateName, nodeName, maxNumber);
            Assert.AreEqual(result, 6);
        }

        [TestMethod]
        public void GetMaxNodeNameNumberingTest4()
        {

            string potentialDuplicateName;
            string nodeName;

            int result;

            potentialDuplicateName = "aaa (4)";
            nodeName = "aaa";
            int maxNumber = 4;

            result = Helpers.GetMaxNodeNameNumbering(potentialDuplicateName, nodeName, maxNumber);
            Assert.AreEqual(result, 4);
        }
    }
}
