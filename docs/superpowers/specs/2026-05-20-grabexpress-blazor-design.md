# Design Spec: GrabExpress Blazor WASM MVP (Direct Port)

*Date:* 2026-05-20  
*Status:* Approved  
*Topic:* Creating a Blazor WebAssembly version of the GrabExpress app by porting existing MAUI logic.

## 1. Goal
To deliver a functional web-based MVP of the GrabExpress application using Blazor WebAssembly. This version will reuse the data structures (Models) and backend logic (Services) from the existing .NET MAUI project via a "Direct Port" strategy to minimize development time, while providing access for Customers, Drivers, and Admins.

## 2. Architecture
The project will follow a "Twin" architecture:
- *Project Name:* GrabExpress.Blazor
- *Type:* Blazor WebAssembly Standalone
- *Hosting:* Client-side (WASM)
- *Data Source:* Shared Firebase instance (Realtime DB, Auth) from the MAUI app.

## 3. Porting Strategy
### 3.1 Code Migration
- *Models:* Copied directly to the new project. Namespaces will be updated to `GrabExpress.Blazor.Models`.
- *Services:* 
  - `DatabaseService` and `FirebaseConfig` copied directly. Namespaces updated.
  - `AuthService` copied with modifications to support web storage.
- *Dependencies:* 
  - `FirebaseDatabase.net` (5.0.0)
  - `FirebaseAuthentication.net` (4.1.0)
  - `Blazored.LocalStorage` (4.5.0) for session persistence.

### 3.2 Authentication & State Management
- Implement a custom `AuthenticationStateProvider` backed by `Blazored.LocalStorage` to persist the Firebase token/user session across browser reloads.
- Use `<AuthorizeView>` to protect routes.

### 3.3 UI & Routing
- Basic, responsive HTML/CSS using standard Blazor components.
- **Routes:**
  - `/login`: User login page.
  - `/register`: User registration page.
  - `/dashboard`: A unified, dynamic dashboard that checks the authenticated user's role (Customer, Driver, Admin) via `DatabaseService` and renders the appropriate sub-component (`CustomerDashboard`, `DriverDashboard`, `AdminDashboard`).

## 4. Success Criteria
- [ ] User can log in/register via the web interface.
- [ ] Users are correctly routed/shown their role-specific dashboard based on their role in the Realtime Database.
- [ ] Authentication state persists across browser refreshes.
- [ ] Models and DatabaseService logic compile and function in the WASM environment.