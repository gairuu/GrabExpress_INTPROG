using Firebase.Auth;
using Firebase.Auth.Providers;
using Newtonsoft.Json;

namespace GrabExpress.Blazor.Services
{
    public class AuthService
    {
        private readonly FirebaseAuthClient _authClient;

        public AuthService()
        {
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
            return await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);
        }

        public async Task<UserCredential> LoginAsync(string email, string password)
        {
            return await _authClient.SignInWithEmailAndPasswordAsync(email, password);
        }

        public void Logout()
        {
            _authClient.SignOut();
        }

        public User? GetCurrentUser()
        {
            return _authClient.User;
        }
    }
}
