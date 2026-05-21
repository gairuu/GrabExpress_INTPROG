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
        private readonly DatabaseService _databaseService;

        public AuthService(ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider, DatabaseService databaseService)
        {
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
            _databaseService = databaseService;

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
            var role = await _databaseService.GetUserRoleAsync(creds.User.Uid) ?? "Customer";

            await _localStorage.SetItemAsync(StorageKeys.AuthToken, token);
            await _localStorage.SetItemAsync(StorageKeys.UserEmail, creds.User.Info.Email);
            await _localStorage.SetItemAsync(StorageKeys.UserUid, creds.User.Uid);
            await _localStorage.SetItemAsync(StorageKeys.UserRole, role);

            if (_authStateProvider is FirebaseAuthStateProvider provider)
            {
                provider.NotifyUserAuthentication(token, creds.User.Info.Email, creds.User.Uid, role);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _authClient.SignOut();
            }
            catch (Exception) { /* Ignore signout errors */ }

            await _localStorage.RemoveItemAsync(StorageKeys.AuthToken);
            await _localStorage.RemoveItemAsync(StorageKeys.UserEmail);
            await _localStorage.RemoveItemAsync(StorageKeys.UserUid);
            await _localStorage.RemoveItemAsync(StorageKeys.UserRole);

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