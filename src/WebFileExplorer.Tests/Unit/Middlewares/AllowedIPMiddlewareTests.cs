using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebFileExplorer.Server;

namespace WebFileExplorer.Tests.Unit.Middlewares
{
    [TestClass]
    [TestCategory("Unit")]
    public class AllowedIPMiddlewareTests
    {
        public class FakeLogger<T> : ILogger<T>
        {
            public string LastLogMessage { get; set; } = "";

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LastLogMessage = formatter(state, exception);
            }
        }

        private class FakeWebHostEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Production";
            public string ApplicationName { get; set; } = "Test";
            public string WebRootPath { get; set; } = "";
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = default!;
            public string ContentRootPath { get; set; } = "";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = default!;
        }

        private IConfiguration CreateConfiguration(string prefix)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string?>("NetworkBinding:AllowedPrefix", prefix) })
                .Build();
        }

        [TestMethod]
        public async Task InvokeAsync_WithNullIpAddress_Returns403()
        {
            // Arrange
            bool nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
            var middleware = new AllowedIPMiddleware(next, CreateConfiguration("10.0.0."), new FakeLogger<AllowedIPMiddleware>(), new FakeWebHostEnvironment());
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = null;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.AreEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.IsFalse(nextCalled);
        }

        [TestMethod]
        [DataRow("10.0.0.5")]
        public async Task InvokeAsync_WithAllowedPrefix_CallsNext(string testIp)
        {
            // Arrange
            bool nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
            var logger = new FakeLogger<AllowedIPMiddleware>();
            var middleware = new AllowedIPMiddleware(next, CreateConfiguration("10.0.0."), logger, new FakeWebHostEnvironment());
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(testIp);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            if (!nextCalled)
            {
                throw new Exception($"Failed. testIp arg: {testIp}. Log: {logger.LastLogMessage}");
            }
            Assert.IsTrue(nextCalled);
        }

        [TestMethod]
        [DataRow("192.168.1.1")]
        [DataRow("172.16.0.1")]
        [DataRow("10.1.2.3")]
        public async Task InvokeAsync_WithDisallowedPrefix_Returns403(string ipAddress)
        {
            // Arrange
            bool nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
            var middleware = new AllowedIPMiddleware(next, CreateConfiguration("10.0.0."), new FakeLogger<AllowedIPMiddleware>(), new FakeWebHostEnvironment());
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.AreEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.IsFalse(nextCalled);
        }

        [TestMethod]
        [DataRow("127.0.0.1")]
        [DataRow("::1")]
        public async Task InvokeAsync_WithLoopbackInDevelopment_CallsNext(string ipAddress)
        {
            // Arrange
            bool nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
            var env = new FakeWebHostEnvironment { EnvironmentName = Environments.Development };
            var middleware = new AllowedIPMiddleware(next, CreateConfiguration("10.0.0."), new FakeLogger<AllowedIPMiddleware>(), env);
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.IsTrue(nextCalled);
        }

        [TestMethod]
        [DataRow("127.0.0.1")]
        [DataRow("::1")]
        public async Task InvokeAsync_WithLoopbackInProduction_Returns403(string ipAddress)
        {
            // Arrange
            bool nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
            var env = new FakeWebHostEnvironment { EnvironmentName = Environments.Production };
            var middleware = new AllowedIPMiddleware(next, CreateConfiguration("10.0.0."), new FakeLogger<AllowedIPMiddleware>(), env);
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.AreEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
            Assert.IsFalse(nextCalled);
        }
    }
}
