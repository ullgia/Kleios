using System.Net;
using Kleios.Frontend.Infrastructure.Services;
using Kleios.Frontend.Shared.Services;
using Kleios.Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace Kleios.Frontend.Tests;

public class AuthenticatedHttpMessageHandlerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthenticatedHttpMessageHandler>> _loggerMock;
    private readonly AuthenticatedHttpMessageHandler _handler;
    private readonly HttpMessageInvoker _invoker;

    public AuthenticatedHttpMessageHandlerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthenticatedHttpMessageHandler>>();
        
        // Create the handler with a test response handler as the inner handler
        var testHandler = new TestHttpMessageHandler();
        _handler = new AuthenticatedHttpMessageHandler(_authServiceMock.Object, _loggerMock.Object)
        {
            InnerHandler = testHandler
        };
        
        // Create an invoker that uses our handler
        _invoker = new HttpMessageInvoker(_handler);
    }

    [Fact]
    public async Task SendAsync_ForAuthEndpoints_DoesNotAddAuthHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://auth-service/api/auth/login");
        
        // Act
        await _invoker.SendAsync(request, CancellationToken.None);
        
        // Assert
        // Verify that GetValidAccessTokenAsync was never called
        _authServiceMock.Verify(s => s.GetValidAccessTokenAsync(), Times.Never);
        // Verify no Authorization header was added
        Assert.False(request.Headers.Contains("Authorization"));
    }

    [Fact]
    public async Task SendAsync_ForNonAuthEndpoints_AddsAuthHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api-service/api/data");
        
        // Setup AuthService to return a valid token
        _authServiceMock.Setup(s => s.GetValidAccessTokenAsync())
            .ReturnsAsync(Option<string>.Success("valid-token"));
        
        // Act
        await _invoker.SendAsync(request, CancellationToken.None);
        
        // Assert
        // Verify that GetValidAccessTokenAsync was called
        _authServiceMock.Verify(s => s.GetValidAccessTokenAsync(), Times.Once);
        // Verify Authorization header was added with correct scheme and token
        Assert.True(request.Headers.Contains("Authorization"));
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("valid-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_When401Received_RetriesRequestWithNewToken()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api-service/api/data");
        
        // Setup inner handler to return 401 and then 200
        var testHandler = new TestHttpMessageHandler();
        testHandler.ResponseQueue.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        testHandler.ResponseQueue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        
        _handler.InnerHandler = testHandler;
        
        // Setup AuthService to return valid tokens
        _authServiceMock.SetupSequence(s => s.GetValidAccessTokenAsync())
            .ReturnsAsync(Option<string>.Success("first-token"))
            .ReturnsAsync(Option<string>.Success("second-token"));
        
        // Act
        var response = await _invoker.SendAsync(request, CancellationToken.None);
        
        // Assert
        // Verify that GetValidAccessTokenAsync was called twice (initial request + retry)
        _authServiceMock.Verify(s => s.GetValidAccessTokenAsync(), Times.Exactly(2));
        // Verify the retry was successful (got a 200 OK)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Verify second token was used for retry
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("second-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_WhenTokenUnavailable_SendsRequestWithoutHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api-service/api/data");
        
        // Setup AuthService to return error when getting token
        _authServiceMock.Setup(s => s.GetValidAccessTokenAsync())
            .ReturnsAsync(Option<string>.ServerError("No token available"));
        
        // Act
        await _invoker.SendAsync(request, CancellationToken.None);
        
        // Assert
        // Verify that GetValidAccessTokenAsync was called
        _authServiceMock.Verify(s => s.GetValidAccessTokenAsync(), Times.Once);
        // Verify no Authorization header was added
        Assert.False(request.Headers.Contains("Authorization"));
    }

    /// <summary>
    /// Test HTTP handler that allows us to queue response messages
    /// </summary>
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> ResponseQueue { get; } = new Queue<HttpResponseMessage>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Return the next queued response or a default 200 OK
            var response = ResponseQueue.Count > 0 
                ? ResponseQueue.Dequeue() 
                : new HttpResponseMessage(HttpStatusCode.OK);
            
            return Task.FromResult(response);
        }
    }
}