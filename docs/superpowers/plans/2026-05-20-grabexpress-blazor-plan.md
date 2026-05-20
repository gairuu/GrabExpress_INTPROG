# GrabExpress Blazor Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a Blazor WebAssembly MVP for GrabExpress that reuses MAUI Models/Services, supports role-based routing, and persists authentication via LocalStorage.

**Architecture:** Blazor WebAssembly Standalone interacting directly with Firebase Realtime Database and Authentication. A custom `AuthenticationStateProvider` bridges Firebase auth tokens with Blazor's authorization system via `Blazored.LocalStorage`.

**Tech Stack:** Blazor WebAssembly, C# 9.0, FirebaseAuthentication.net (4.1.0), FirebaseDatabase.net (5.0.0), Blazored.LocalStorage (4.5.0).

---

## File Structure Map
- `GrabExpress.Blazor/Services/FirebaseAuthStateProvider.cs` - Custom AuthenticationStateProvider.
- `GrabExpress.Blazor/Services/AuthService.cs` - (Modify) Add LocalStorage integration.
- `GrabExpress.Blazor/Program.cs` - (Modify) Register services and authorization.
- `GrabExpress.Blazor/App.razor` - (Modify) Add CascadingAuthenticationState.
- `GrabExpress.Blazor/Layout/NavMenu.razor` - (Modify) Update navigation links based on auth state.
- `GrabExpress.Blazor/Layout/MainLayout.razor` - (Modify) Wrap with AuthorizeView if needed or just handle layout.
- `GrabExpress.Blazor/Pages/Login.razor` - Login page.
- `GrabExpress.Blazor/Pages/Register.razor` - Registration page.
- `GrabExpress.Blazor/Pages/Dashboard.razor` - Unified dashboard routing based on role.

---

### Task 1: Update Namespace and Fix MAUI references in Models & Services

**Files:**
- Modify: `GrabExpress.Blazor/Models/*.cs`
- Modify: `GrabExpress.Blazor/Services/*.cs`

- [ ] **Step 1: Replace namespaces**

Run: `find GrabExpress.Blazor/Models GrabExpress.Blazor/Services -name "*.cs" -exec sed -i 's/namespace GrabExpress_INTPROG/namespace GrabExpress.Blazor/g' {} +`
(Note: If sed is not available, we will use the replace tool on each file: Admin.cs, Customer.cs, Delivery.cs, Driver.cs, Payment.cs, UserProfile.cs, Vehicle.cs, AuthService.cs, DatabaseService.cs, FirebaseConfig.cs)

- [ ] **Step 2: Commit namespace updates**

```bash
git add GrabExpress.Blazor/Models GrabExpress.Blazor/Services
git commit -m "refactor: update namespaces to GrabExpress.Blazor"
```

---

### Task 2: Implement FirebaseAuthStateProvider

**Files:**
- Create: `GrabExpress.Blazor/Services/FirebaseAuthStateProvider.cs`

- [ ] **Step 1: Write FirebaseAuthStateProvider code**

Create `GrabExpress.Blazor/Services/FirebaseAuthStateProvider.cs`:
```csharp
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GrabExpress.Blazor.Services
{
    public class FirebaseAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        public FirebaseAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            var savedEmail = await _localStorage.GetItemAsync<string>("userEmail");

            if (string.IsNullOrWhiteSpace(savedToken))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, savedEmail ?? "user@example.com"),
                new Claim("FirebaseToken", savedToken)
            };

            var identity = new ClaimsIdentity(claims, "Firebase");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public void NotifyUserAuthentication(string token, string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("FirebaseToken", token)
            };

            var identity = new ClaimsIdentity(claims, "Firebase");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
    }
}
```

- [ ] **Step 2: Commit FirebaseAuthStateProvider**

```bash
git add GrabExpress.Blazor/Services/FirebaseAuthStateProvider.cs
git commit -m "feat: add FirebaseAuthStateProvider"
```

---

### Task 3: Adapt AuthService for Blazor

**Files:**
- Modify: `GrabExpress.Blazor/Services/AuthService.cs`

- [ ] **Step 1: Modify AuthService to use LocalStorage**

Update `GrabExpress.Blazor/Services/AuthService.cs` to inject `ILocalStorageService` and `AuthenticationStateProvider`.

```csharp
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

            ((FirebaseAuthStateProvider)_authStateProvider).NotifyUserAuthentication(token, creds.User.Info.Email);
        }

        public async Task LogoutAsync()
        {
            _authClient.SignOut();
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userEmail");
            await _localStorage.RemoveItemAsync("userUid");

            ((FirebaseAuthStateProvider)_authStateProvider).NotifyUserLogout();
        }

        public User? GetCurrentUser()
        {
            return _authClient.User;
        }
    }
}
```

- [ ] **Step 2: Commit AuthService update**

```bash
git add GrabExpress.Blazor/Services/AuthService.cs
git commit -m "feat: adapt AuthService for web storage and auth state"
```

---

### Task 4: Configure App Services and Authentication

**Files:**
- Modify: `GrabExpress.Blazor/Program.cs`
- Modify: `GrabExpress.Blazor/App.razor`
- Modify: `GrabExpress.Blazor/_Imports.razor`

- [ ] **Step 1: Register Services in Program.cs**

Update `GrabExpress.Blazor/Program.cs`:
```csharp
using Blazored.LocalStorage;
using GrabExpress.Blazor;
using GrabExpress.Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, FirebaseAuthStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DatabaseService>();

await builder.Build().RunAsync();
```

- [ ] **Step 2: Add CascadingAuthenticationState to App.razor**

Update `GrabExpress.Blazor/App.razor`:
```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)">
                <NotAuthorized>
                    <p role="alert">You are not authorized to access this page.</p>
                </NotAuthorized>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(Layout.MainLayout)">
                <p role="alert">Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

- [ ] **Step 3: Update _Imports.razor**

Add to `GrabExpress.Blazor/_Imports.razor`:
```razor
@using Microsoft.AspNetCore.Components.Authorization
@using Blazored.LocalStorage
@using GrabExpress.Blazor.Services
@using GrabExpress.Blazor.Models
```

- [ ] **Step 4: Commit Program.cs and App.razor updates**

```bash
git add GrabExpress.Blazor/Program.cs GrabExpress.Blazor/App.razor GrabExpress.Blazor/_Imports.razor
git commit -m "config: setup DI, auth state, and imports"
```

---

### Task 5: Implement Login Page

**Files:**
- Create: `GrabExpress.Blazor/Pages/Login.razor`

- [ ] **Step 1: Create Login.razor**

Create `GrabExpress.Blazor/Pages/Login.razor`:
```razor
@page "/login"
@inject AuthService AuthService
@inject NavigationManager NavigationManager

<h3>Login</h3>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}

<div class="form-group mb-3">
    <label>Email:</label>
    <input type="email" class="form-control" @bind="email" />
</div>

<div class="form-group mb-3">
    <label>Password:</label>
    <input type="password" class="form-control" @bind="password" />
</div>

<button class="btn btn-primary" @onclick="HandleLogin">Login</button>
<p class="mt-3">Don't have an account? <a href="/register">Register</a></p>

@code {
    private string email = "";
    private string password = "";
    private string errorMessage = "";

    private async Task HandleLogin()
    {
        try
        {
            errorMessage = "";
            await AuthService.LoginAsync(email, password);
            NavigationManager.NavigateTo("/dashboard");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }
}
```

- [ ] **Step 2: Commit Login Page**

```bash
git add GrabExpress.Blazor/Pages/Login.razor
git commit -m "feat: add login page"
```

---

### Task 6: Implement Register Page

**Files:**
- Create: `GrabExpress.Blazor/Pages/Register.razor`

- [ ] **Step 1: Create Register.razor**

Create `GrabExpress.Blazor/Pages/Register.razor`:
```razor
@page "/register"
@inject AuthService AuthService
@inject DatabaseService DatabaseService
@inject NavigationManager NavigationManager
@using Firebase.Auth

<h3>Register</h3>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}

<div class="form-group mb-3">
    <label>Email:</label>
    <input type="email" class="form-control" @bind="email" />
</div>

<div class="form-group mb-3">
    <label>Password:</label>
    <input type="password" class="form-control" @bind="password" />
</div>

<div class="form-group mb-3">
    <label>Full Name:</label>
    <input type="text" class="form-control" @bind="fullName" />
</div>

<button class="btn btn-primary" @onclick="HandleRegister">Register</button>
<p class="mt-3">Already have an account? <a href="/login">Login</a></p>

@code {
    private string email = "";
    private string password = "";
    private string fullName = "";
    private string errorMessage = "";

    private async Task HandleRegister()
    {
        try
        {
            errorMessage = "";
            var creds = await AuthService.RegisterAsync(email, password);
            
            // Create default customer profile
            var customer = new Customer
            {
                Name = fullName,
                Email = email,
                Phone = "", // Collect later
                Role = "Customer"
            };
            
            await DatabaseService.SaveUserProfileAsync(creds.User.Uid, customer);
            await DatabaseService.SaveUserRoleAsync(creds.User.Uid, "Customer");

            NavigationManager.NavigateTo("/dashboard");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }
}
```

- [ ] **Step 2: Commit Register Page**

```bash
git add GrabExpress.Blazor/Pages/Register.razor
git commit -m "feat: add register page"
```

---

### Task 7: Implement Role-Based Dashboard

**Files:**
- Create: `GrabExpress.Blazor/Pages/Dashboard.razor`

- [ ] **Step 1: Create Dashboard.razor**

Create `GrabExpress.Blazor/Pages/Dashboard.razor`:
```razor
@page "/dashboard"
@attribute [Authorize]
@inject DatabaseService DatabaseService
@inject ILocalStorageService LocalStorage

<h3>Dashboard</h3>

@if (isLoading)
{
    <p>Loading your dashboard...</p>
}
else if (userRole == "Admin")
{
    <h4>Welcome Admin</h4>
    <p>Admin features coming soon...</p>
}
else if (userRole == "Driver")
{
    <h4>Welcome Driver</h4>
    <p>Driver features coming soon...</p>
}
else if (userRole == "Customer" || userRole == null)
{
    <h4>Welcome Customer</h4>
    <p>Customer features coming soon...</p>
}

@code {
    private bool isLoading = true;
    private string? userRole;

    protected override async Task OnInitializedAsync()
    {
        var uid = await LocalStorage.GetItemAsync<string>("userUid");
        if (!string.IsNullOrEmpty(uid))
        {
            userRole = await DatabaseService.GetUserRoleAsync(uid);
            // Remove outer quotes if returned by Firebase
            if (!string.IsNullOrEmpty(userRole) && userRole.StartsWith("\"") && userRole.EndsWith("\""))
            {
                userRole = userRole.Substring(1, userRole.Length - 2);
            }
        }
        isLoading = false;
    }
}
```

- [ ] **Step 2: Update NavMenu.razor**

Update `GrabExpress.Blazor/Layout/NavMenu.razor` to include Dashboard, Login, and Logout logic.

Replace the content with:
```razor
<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">GrabExpress</a>
    </div>
</div>

<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </NavLink>
        </div>
        
        <AuthorizeView>
            <Authorized>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="dashboard">
                        <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Dashboard
                    </NavLink>
                </div>
                <div class="nav-item px-3">
                    <button class="nav-link btn btn-link" @onclick="Logout">
                        <span class="bi bi-box-arrow-right-nav-menu" aria-hidden="true"></span> Logout
                    </button>
                </div>
            </Authorized>
            <NotAuthorized>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="login">
                        <span class="bi bi-person-nav-menu" aria-hidden="true"></span> Login
                    </NavLink>
                </div>
            </NotAuthorized>
        </AuthorizeView>
    </nav>
</div>

@code {
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private async Task Logout()
    {
        await AuthService.LogoutAsync();
        NavigationManager.NavigateTo("/");
    }
}
```

- [ ] **Step 3: Commit Dashboard and NavMenu**

```bash
git add GrabExpress.Blazor/Pages/Dashboard.razor GrabExpress.Blazor/Layout/NavMenu.razor
git commit -m "feat: add role-based dashboard and update navigation"
```