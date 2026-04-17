using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace WebFileExplorer.Tests.Integration.Api
{
    [TestClass]
    public class NetworkBindingTests
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void Startup_InDevelopment_DoesNotThrow()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>();

            // Act
            var client = factory.CreateClient();

            // Assert
            Assert.IsNotNull(client, "Expected the application to start and provide an HttpClient.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void Startup_InProduction_WithInvalidPrefix_ThrowsInvalidOperationException()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                    builder.UseSetting("NetworkBinding:AllowedPrefix", "255.255.255.");
                });

            // Act & Assert
            global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsExactly<InvalidOperationException>(() => { factory.CreateClient(); });
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task AllowedIPMiddleware_ValidIP_ReturnsOk()
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                    builder.UseSetting("NetworkBinding:AllowedPrefix", "10.0.0.");
                });

            _ = factory.CreateDefaultClient();

            // Hit a known controller endpoint so a happy path produces 200.
            var response = await factory.Server.SendAsync(context =>
            {
                context.Request.Path = "/api/fileexplorer/roots";
                context.Request.Method = HttpMethods.Get;
                context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
            });

            Assert.AreEqual(
                (int)System.Net.HttpStatusCode.OK,
                response.Response.StatusCode,
                $"Expected the allow-listed IP to reach the controller and return 200; got {response.Response.StatusCode}.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task AllowedIPMiddleware_InvalidIP_ReturnsForbidden()
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                    builder.UseSetting("NetworkBinding:AllowedPrefix", "10.0.0.");
                });

            var response = await factory.Server.SendAsync(context => 
            {
                context.Request.Path = "/";
                context.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");
            });

            Assert.AreEqual((int)System.Net.HttpStatusCode.Forbidden, response.Response.StatusCode);
        }
    }
}
