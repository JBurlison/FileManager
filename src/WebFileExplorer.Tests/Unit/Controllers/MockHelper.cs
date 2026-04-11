using Moq;
using WebFileExplorer.Server.Services;

namespace WebFileExplorer.Tests.Unit.Controllers
{
    public static class MockHelper
    {
        public static IRecycleBinService GetMockRecycleBinService()
        {
            var mock = new Mock<IRecycleBinService>();
            mock.Setup(m => m.IsSupported).Returns(true);
            mock.Setup(m => m.MoveToRecycleBin(It.IsAny<string>())).Returns(true);
            mock.Setup(m => m.EmptyBin()).Returns(true);
            mock.Setup(m => m.RestoreItem(It.IsAny<string>())).Returns(true);
            return mock.Object;
        }
    }
}
