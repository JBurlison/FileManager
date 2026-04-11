using Moq;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Services
{
    [TestClass]
    public class RecycleBinServiceTests
    {
        [TestMethod]
        public void GetDeletedItems_ReturnsRecycleBinItems()
        {
            // Arrange
            var mockShell = new Mock<IWindowsShellService>();
            mockShell.Setup(s => s.GetDeletedItems()).Returns(new List<RecycleBinItem> { 
                new RecycleBinItem("Item1.txt", "C:\\Recycled\\Item1.txt", DateTimeOffset.Now, "ID1", 1024) 
            });
            var service = new RecycleBinService(mockShell.Object);

            // Act
            var items = service.GetDeletedItems();

            // Assert
            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual("Item1.txt", items.First().Name);
        }

        [TestMethod]
        public void MoveToRecycleBin_CallsShellService()
        {
            var mockShell = new Mock<IWindowsShellService>();
            mockShell.Setup(s => s.MoveToRecycleBin("C:\\test.txt")).Returns(true);
            var service = new RecycleBinService(mockShell.Object);

            var result = service.MoveToRecycleBin("C:\\test.txt");

            Assert.IsTrue(result);
            mockShell.Verify(s => s.MoveToRecycleBin("C:\\test.txt"), Times.Once);
        }
        
        [TestMethod]
        public void RestoreItem_CallsShellService()
        {
            var mockShell = new Mock<IWindowsShellService>();
            mockShell.Setup(s => s.RestoreItem("ID1")).Returns(true);
            var service = new RecycleBinService(mockShell.Object);

            var result = service.RestoreItem("ID1");

            Assert.IsTrue(result);
            mockShell.Verify(s => s.RestoreItem("ID1"), Times.Once);
        }
        
        [TestMethod]
        public void EmptyBin_CallsShellService()
        {
            var mockShell = new Mock<IWindowsShellService>();
            mockShell.Setup(s => s.EmptyBin()).Returns(true);
            var service = new RecycleBinService(mockShell.Object);

            var result = service.EmptyBin();

            Assert.IsTrue(result);
            mockShell.Verify(s => s.EmptyBin(), Times.Once);
        }

        [TestMethod]
        public void IsSupported_ReturnsShellServiceStatus()
        {
            var mockShell = new Mock<IWindowsShellService>();
            mockShell.Setup(s => s.IsSupported).Returns(true);
            var service = new RecycleBinService(mockShell.Object);

            var result = service.IsSupported;

            Assert.IsTrue(result);
        }
    }
}
