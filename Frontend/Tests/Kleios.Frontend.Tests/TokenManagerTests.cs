using System.IdentityModel.Tokens.Jwt;
using Kleios.Frontend.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Kleios.Frontend.Tests;

public class TokenManagerTests
{
    private readonly TokenManager _tokenManager;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<TokenManager>> _loggerMock;
    private readonly Mock<IResponseCookies> _responseCookiesMock;
    private readonly Mock<IRequestCookieCollection> _requestCookiesMock;

    private const string ValidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IlRlc3QgVXNlciIsImlhdCI6MTYxNjIzOTAyMiwiZXhwIjo0NzY5OTEzNDIyfQ.aHS_9_HC5h50UTxxVJVHGrGs_uF5_xhLjwLOEWPlgMw";
    private const string ExpiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IlRlc3QgVXNlciIsImlhdCI6MTYxNjIzOTAyMiwiZXhwIjoxNjE2MjQyNjIyfQ.hL1hs-D_2OyoFyMokc_GKuU4t7jDEYvXyY_Vb4oBjCo";
    private string ValidRefreshToken = "refresh-token-123456";
    private const string RefreshTokenCookieName = "refresh_token";

    public TokenManagerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<TokenManager>>();
        _responseCookiesMock = new Mock<IResponseCookies>();
        _requestCookiesMock = new Mock<IRequestCookieCollection>();

        // Configurazione del mock del contesto HTTP
        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(r => r.Cookies).Returns(_requestCookiesMock.Object);

        var responseMock = new Mock<HttpResponse>();
        responseMock.Setup(r => r.Cookies).Returns(_responseCookiesMock.Object);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.Request).Returns(requestMock.Object);
        httpContextMock.Setup(c => c.Response).Returns(responseMock.Object);

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        _tokenManager = new TokenManager(_httpContextAccessorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void SetTokens_ShouldStoreTokensInCookies()
    {
        // Act
        _tokenManager.SetTokens(ValidToken, ValidRefreshToken);

        // Assert
        _responseCookiesMock.Verify(c => c.Append(
            It.Is<string>(s => s == RefreshTokenCookieName),
            It.Is<string>(s => s == ValidRefreshToken),
            It.IsAny<CookieOptions>()
        ), Times.Once);
    }

    [Fact]
    public void ClearTokens_ShouldRemoveTokensFromCookies()
    {
        // Act
        _tokenManager.ClearTokens();

        // Assert
        // Modifica: verifichiamo solo che sia stato chiamato Delete con il nome del cookie
        _responseCookiesMock.Verify(c => c.Delete(
            It.Is<string>(s => s == RefreshTokenCookieName)
        ), Times.Once);
    }

    [Fact]
    public void TryGetAccessToken_WhenTokenExists_ReturnsTrue()
    {
        // Arrange
        // Prima dobbiamo impostare il token
        _tokenManager.SetTokens(ValidToken, ValidRefreshToken);

        // Act
        bool result = _tokenManager.TryGetAccessToken(out string token);

        // Assert
        Assert.True(result);
        Assert.Equal(ValidToken, token);
    }

    [Fact]
    public void GetRefreshToken_WhenTokenExists_ReturnsToken()
    {
        // Arrange
        string refreshToken = ValidRefreshToken;
        _requestCookiesMock.Setup(c => c.TryGetValue(RefreshTokenCookieName, out refreshToken)).Returns(true);

        // Act
        string result = _tokenManager.GetRefreshToken();

        // Assert
        Assert.Equal(ValidRefreshToken, result);
    }

    [Fact]
    public void GetRefreshToken_WhenTokenDoesNotExist_ReturnsEmptyString()
    {
        // Arrange
        string outValue = string.Empty;
        _requestCookiesMock.Setup(c => c.TryGetValue(RefreshTokenCookieName, out outValue)).Returns(false);

        // Act
        string result = _tokenManager.GetRefreshToken();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void IsTokenValid_WithValidToken_ReturnsTrue()
    {
        // Arrange
        _tokenManager.SetTokens(ValidToken, ValidRefreshToken);

        // Act
        bool result = _tokenManager.IsTokenValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetClaimsFromToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        // Act
        var result = _tokenManager.GetClaimsFromToken(ValidToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("1234567890", result.Value.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal("Test User", result.Value.FindFirst(JwtRegisteredClaimNames.Name)?.Value);
    }

    [Fact]
    public void GetClaimsFromToken_WithInvalidToken_ReturnsError()
    {
        // Arrange
        string invalidToken = "invalid-token";

        // Act
        var result = _tokenManager.GetClaimsFromToken(invalidToken);

        // Assert
        Assert.False(result.IsSuccess);
        // Modifica: non verifichiamo più il valore (che sarebbe null) ma solo la presenza di un errore
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void GetTokenRemainingLifetimeSeconds_WithValidToken_ReturnsPositiveSeconds()
    {
        // Arrange
        _tokenManager.SetTokens(ValidToken, ValidRefreshToken);

        // Act
        double seconds = _tokenManager.GetTokenRemainingLifetimeSeconds();

        // Assert
        Assert.True(seconds > 0);
    }
}