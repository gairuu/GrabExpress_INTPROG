using Blazored.LocalStorage;
using GrabExpress.Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GrabExpress.Blazor.Tests
{
    public class FirebaseAuthStateProviderTests
    {
        private readonly Mock<ILocalStorageService> _mockLocalStorage;
        private readonly FirebaseAuthStateProvider _authStateProvider;

        public FirebaseAuthStateProviderTests()
        {
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _authStateProvider = new FirebaseAuthStateProvider(_mockLocalStorage.Object);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_NoToken_ReturnsAnonymous()
        {
            // Arrange
            _mockLocalStorage.Setup(x => x.GetItemAsync<string>("authToken", default))
                .ReturnsAsync((string)null);

            // Act
            var result = await _authStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.False(result.User.Identity.IsAuthenticated);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_WithToken_ReturnsAuthenticated()
        {
            // Arrange
            var token = "fake-token";
            var email = "test@example.com";
            _mockLocalStorage.Setup(x => x.GetItemAsync<string>("authToken", default))
                .ReturnsAsync(token);
            _mockLocalStorage.Setup(x => x.GetItemAsync<string>("userEmail", default))
                .ReturnsAsync(email);

            // Act
            var result = await _authStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.True(result.User.Identity.IsAuthenticated);
            Assert.Equal(email, result.User.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal(token, result.User.FindFirst("FirebaseToken")?.Value);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_WithTokenAndRole_ReturnsAuthenticatedWithRole()
        {
            // Arrange
            var token = "fake-token";
            var email = "test@example.com";
            var role = "Admin";
            _mockLocalStorage.Setup(x => x.GetItemAsync<string>("authToken", default))
                .ReturnsAsync(token);
            _mockLocalStorage.Setup(x => x.GetItemAsync<string>("userEmail", default))
                .ReturnsAsync(email);
            _mockLocalStorage.Setup(x => x.GetItemAsync<string>("userRole", default))
                .ReturnsAsync(role);

            // Act
            var result = await _authStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.True(result.User.Identity.IsAuthenticated);
            Assert.Equal(role, result.User.FindFirst(ClaimTypes.Role)?.Value);
        }
    }
}
