using Blazored.LocalStorage;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Microsoft.AspNetCore.Components.Authorization;

namespace GrabExpress.Blazor.Services
{
    public class AuthService
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
        {
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;

            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = "grabexpress-1267a.firebaseapp.com",
                Providers = new[]
                {
                    new EmailProvider()
                }
            };

            _authClient = new FirebaseAuthClient(config);
        }

        public async Task<UserCredential> RegisterAsync(string email, string password)
        {
            var creds = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);
            await HandleLoginSuccess(creds);
            return creds;
        }

        public async Task<UserCredential> LoginAsync(string email, string password)
        {
            var creds = await _authClient.SignInWithEmailAndPasswordAsync(email, password);
            await HandleLoginSuccess(creds);
            return creds;
        }

        private async Task HandleLoginSuccess(UserCredential creds)
        {
            var token = await creds.User.GetIdTokenAsync();
            await _localStorage.SetItemAsync("authToken", token);
            await _localStorage.SetItemAsync("userEmail", creds.User.Info.Email);
            await _localStorage.SetItemAsync("userUid", creds.User.Uid);

            if (_authStateProvider is FirebaseAuthStateProvider provider)
            {
                provider.NotifyUserAuthentication(token, creds.User.Info.Email);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _authClient.SignOut();
            }
            catch (Exception) { /* Ignore signout errors */ }

            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userEmail");
            await _localStorage.RemoveItemAsync("userUid");

            if (_authStateProvider is FirebaseAuthStateProvider provider)
            {
                provider.NotifyUserLogout();
            }
        }

        public User? GetCurrentUser()
        {
            return _authClient.User;
        }
    }
}