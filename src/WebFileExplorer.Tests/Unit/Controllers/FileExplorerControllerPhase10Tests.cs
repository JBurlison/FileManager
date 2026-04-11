using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers
{
    [TestClass]
    public class FileExplorerControllerPhase10Tests
    {
        private Mock<IFileSystemProvider> _mockFileSystemProvider = null!;
        private Mock<IArchiveService> _mockArchiveService = null!;
        private Mock<IRecycleBinService> _mockRecycleBinService = null!;
        private Mock<ILogger<FileExplorerController>> _mockLogger = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockFileSystemProvider = new Mock<IFileSystemProvider>();
            _mockArchiveService = new Mock<IArchiveService>();
            _mockRecycleBinService = new Mock<IRecycleBinService>();
            _mockRecycleBinService.Setup(s => s.IsSupported).Returns(true);
            _mockLogger = new Mock<ILogger<FileExplorerController>>();
        }

        private FileExplorerController CreateController()
        {
            return new FileExplorerController(
                _mockFileSystemProvider.Object,
                _mockArchiveService.Object,
                _mockRecycleBinService.Object,
                _mockLogger.Object
            );
        }

        [TestMethod]
        public void GetRecycleBin_ReturnsOkObjectResult_WithRecycleBinItems()
        {
            // Arrange
            var controller = CreateController();
            var expectedItems = new List<RecycleBinItem> { new RecycleBinItem("Test", "C:\\Test", DateTimeOffset.Now, "123", 100) };
            _mockRecycleBinService.Setup(s => s.GetDeletedItems()).Returns(expectedItems);

            // Act
            var result = controller.GetRecycleBin();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(expectedItems, okResult.Value);
        }

        [TestMethod]
        public void RestoreRecycleBinItem_ReturnsOkResult()
        {
            // Arrange
            var controller = CreateController();
            _mockRecycleBinService.Setup(s => s.RestoreItem("123")).Returns(true);

            // Act
            var result = controller.RestoreRecycleBinItem("123");

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }
        
        [TestMethod]
        public void EmptyRecycleBin_ReturnsOkResult()
        {
            // Arrange
            var controller = CreateController();
            _mockRecycleBinService.Setup(s => s.EmptyBin()).Returns(true);

            // Act
            var result = controller.EmptyRecycleBin();

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void GetRecycleBin_WhenNotSupported_Returns501()
        {
            var controller = CreateController();
            _mockRecycleBinService.Setup(s => s.IsSupported).Returns(false);

            var result = controller.GetRecycleBin();

            var statusResult = result.Result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(501, statusResult.StatusCode);
            Assert.AreEqual("Recycle Bin is unsupported or unavailable on this system.", statusResult.Value);
        }

        [TestMethod]
        public void RestoreRecycleBinItem_WhenNotSupported_Returns501()
        {
            var controller = CreateController();
            _mockRecycleBinService.Setup(s => s.IsSupported).Returns(false);

            var result = controller.RestoreRecycleBinItem("123");

            var statusResult = result.Result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(501, statusResult.StatusCode);
        }

        [TestMethod]
        public void EmptyRecycleBin_WhenNotSupported_Returns501()
        {
            var controller = CreateController();
            _mockRecycleBinService.Setup(s => s.IsSupported).Returns(false);

            var result = controller.EmptyRecycleBin();

            var statusResult = result.Result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(501, statusResult.StatusCode);
        }

        [TestMethod]
        public void DeleteRecycleBinItem_WhenNotSupported_Returns501()
        {
            var controller = CreateController();
            _mockRecycleBinService.Setup(s => s.IsSupported).Returns(false);

            var result = controller.DeleteRecycleBinItem("123");

            var statusResult = result.Result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(501, statusResult.StatusCode);
        }
    }
}
