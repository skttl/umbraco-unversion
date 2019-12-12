using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Umbraco.Core.Models;

namespace Our.Umbraco.UnVersion.Tests
{
    public static class TestHelper
    {
        public static Mock<IContent> GetVersionMock(int versionId, DateTime updateDate)
        {
            var mock = new Mock<IContent>();
            mock.Setup(x => x.VersionId).Returns(versionId);
            mock.Setup(x => x.UpdateDate).Returns(updateDate);
            return mock;
        }
    }
}
