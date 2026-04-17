using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Client.Components;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class PreviewDialogTests : BunitContext
{
    private Func<HttpRequestMessage, HttpResponseMessage>? _mockHandler;

    [TestInitialize]
    public void Setup()
    {
        var httpMessageHandler = new CustomMockHttpMessageHandler(req => _mockHandler != null ? _mockHandler(req) : new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);
    }

    [TestMethod]
    public void PreviewDialog_WithImage_SetsImageUrl()
    {
        // Act
        var cut = Render<PreviewDialog>(parameters => parameters
            .Add(p => p.FilePath, "C:\\Photos\\image.jpg")
            .Add(p => p.FileName, "image.jpg")
        );

        // Assert
        cut.WaitForState(() => !cut.Markup.Contains("RadzenProgressBar"));
        var img = cut.Find("img");
        Assert.IsNotNull(img);
        var src = img.GetAttribute("src");
        Assert.IsNotNull(src);
        StringAssert.Contains(src, "api/fileexplorer/download");
        StringAssert.Contains(src, "path=C%3A%5CPhotos%5Cimage.jpg");
        StringAssert.Contains(src, "inline=true");
    }

    [TestMethod]
    public void PreviewDialog_WithText_LoadsTextContent()
    {
        // Arrange
        _mockHandler = request =>
        {
            if (request.RequestUri != null && request.RequestUri.PathAndQuery.Contains("download"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Hello World text")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var cut = Render<PreviewDialog>(parameters => parameters
            .Add(p => p.FilePath, "C:\\Docs\\notes.txt")
            .Add(p => p.FileName, "notes.txt")
        );

        // Assert
        cut.WaitForState(() => !cut.Markup.Contains("RadzenProgressBar"));
        var textArea = cut.Find("textarea");
        Assert.IsNotNull(textArea);
        Assert.AreEqual("Hello World text", textArea.TextContent);
    }

    [TestMethod]
    public void PreviewDialog_LargeFile_ShowsSizeWarning()
    {
        // Arrange — return a body noticeably larger than the 100 KB preview cutoff.
        var largeBody = new string('x', 150 * 1024);
        _mockHandler = request =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(largeBody)
            };
        };

        // Act
        var cut = Render<PreviewDialog>(parameters => parameters
            .Add(p => p.FilePath, "C:\\Docs\\big.log")
            .Add(p => p.FileName, "big.log")
        );

        // Assert
        cut.WaitForState(() => !cut.Markup.Contains("RadzenProgressBar"));
        var textArea = cut.Find("textarea");
        Assert.IsNotNull(textArea);
        StringAssert.Contains(textArea.TextContent, "Preview truncated at 100KB");
        // Preview body itself must stay at or below the cutoff.
        Assert.IsTrue(textArea.TextContent.Length < largeBody.Length,
            "Preview should be truncated below the full body size.");
    }

    [TestMethod]
    public void PreviewDialog_HttpError_ShowsErrorMessage()
    {
        // Arrange
        _mockHandler = request =>
            new HttpResponseMessage(HttpStatusCode.Forbidden);

        // Act
        var cut = Render<PreviewDialog>(parameters => parameters
            .Add(p => p.FilePath, "C:\\Docs\\secret.txt")
            .Add(p => p.FileName, "secret.txt")
        );

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Failed to load file."));
        StringAssert.Contains(cut.Markup, "Forbidden");
    }
}
