using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Our.Umbraco.UnVersion.Services;
using Umbraco.Core.Models;

namespace Our.Umbraco.UnVersion.Tests.Services
{
    [TestClass]
    public class ServiceTests
    {
        [TestMethod]
        public void GetVersions_Returns_Right_Based_On_Date()
        {

            var config = new UnVersionConfigEntry() {MaxDays = 10};

            List<IContent> versions = new List<IContent>()
            {
                TestHelper.GetVersionMock(1, new DateTime(2019, 12, 10)).Object, // should be deleted
                TestHelper.GetVersionMock(2, new DateTime(2019, 12, 19)).Object, // should be deleted
                TestHelper.GetVersionMock(3, new DateTime(2019, 12, 20)).Object // should be kept
            };

            var service = new UnVersionService(null,null,null,null,null,null);

            var res = service.GetVersionsToDelete(versions, config, new DateTime(2019, 12, 30));

            Assert.IsTrue(res.Contains(1));
            Assert.IsTrue(res.Contains(2));
            Assert.IsFalse(res.Contains(3));

        }

        [TestMethod]
        public void GetVersions_Returns_Right_Based_Max_Count()
        {
            var config = new UnVersionConfigEntry() { MaxCount = 5 };

            List<IContent> versions = new List<IContent>()
            {
                TestHelper.GetVersionMock(10, new DateTime(2019, 12, 10)).Object, // should be kept
                TestHelper.GetVersionMock(20, new DateTime(2019, 12, 19)).Object, // should be kept
                TestHelper.GetVersionMock(30, new DateTime(2019, 12, 20)).Object, // should be kept
                TestHelper.GetVersionMock(40, new DateTime(2019, 12, 10)).Object, // should be kept
                TestHelper.GetVersionMock(50, new DateTime(2019, 12, 19)).Object, // should be kept
                TestHelper.GetVersionMock(60, new DateTime(2019, 12, 20)).Object, // should be deleted
                TestHelper.GetVersionMock(70, new DateTime(2019, 12, 10)).Object, // should be deleted
                TestHelper.GetVersionMock(80, new DateTime(2019, 12, 19)).Object, // should be deleted
                TestHelper.GetVersionMock(90, new DateTime(2019, 12, 20)).Object, // should be deleted
            };

            var service = new UnVersionService(null,null,null,null,null,null);

            var res = service.GetVersionsToDelete(versions, config, new DateTime(2019, 12, 30));

            Assert.IsFalse(res.Contains(50));

            Assert.IsTrue(res.Contains(60));
            Assert.IsTrue(res.Contains(70));
            Assert.IsTrue(res.Contains(80));
            Assert.IsTrue(res.Contains(90));

        }

    }
}
