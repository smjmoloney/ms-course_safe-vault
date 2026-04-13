# SafeVault

A Blazor WebAssembly + ASP.NET Core project demonstrating secure coding practices in .NET, built with GitHub Copilot assistance.

## Project Structure

- **SafeVaultClient** — Blazor WebAssembly front-end (login, user registration, security test pages)
- **SafeVaultServer** — ASP.NET Core minimal API back-end (Identity, JWT authentication, SQLite)
- **SafeVaultShared** — Shared class library containing input validation logic
- **SaveValueTest** — Console-based test harness for validation methods

## GitHub Repository

This project is hosted on GitHub as a course deliverable.

## Input Validation & SQL Injection Prevention

Copilot was used to generate the `InputValidation` class in `SafeVaultShared`, which provides:

- **`IsValidInput(string input, string allowedSpecialChars)`** — Whitelists alphanumeric characters plus an explicit set of allowed special characters. Any input containing SQL metacharacters (`'`, `;`, `--`), HTML tags (`<`, `>`), or unexpected symbols is rejected.
- **`IsValidPassword(string password)`** — Extends `IsValidInput` with a controlled set of special characters (`!@#$%^&*?`).

These methods are called on both the client (for immediate feedback in `SecurityTests.razor`) and the server (in `AuthController` before any Identity operation), ensuring defence in depth. ASP.NET Core Identity further uses parameterised queries via Entity Framework Core, eliminating SQL injection at the data layer.

## Authentication & Authorisation (RBAC)

The application implements full role-based access control:

- **ASP.NET Core Identity** manages users and roles (`Admin`, `Employee`), stored in SQLite via EF Core.
- **JWT Bearer tokens** are issued on login (`/api/auth/login`) with a `role` claim. Tokens expire after 60 minutes.
- **Server-side authorisation** — The register endpoint (`/api/auth/register`) is protected with an `AdminOnly` policy via `RequireAuthorization`.
- **Client-side authorisation** — `AuthorizeRouteView` and `AuthorizeView` components restrict UI based on roles. The user registration form is only visible to Admins. Unauthenticated users are redirected to `/login`.
- **Token persistence** — JWTs are stored in `localStorage` so sessions survive page refreshes. The `AppAuthStateProvider` restores authentication state on startup.

Copilot assisted with wiring up `AddIdentityCore`, configuring `JwtBearerDefaults`, generating the `AuthController` with token generation, and creating the client-side `AppAuthStateProvider` and `AuthService`.

## Vulnerabilities Identified & Fixes Applied

| Vulnerability                    | How It Was Found                                                                                                      | Fix Applied                                                                                                                                                                 |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **SQL Injection**                | Tested with payloads like `admin' OR '1'='1` and `'; DROP TABLE Users; --` via the Security Tests page                | `InputValidation.IsValidInput` rejects any input with SQL metacharacters; EF Core uses parameterised queries throughout                                                     |
| **XSS (Cross-Site Scripting)**   | Tested with `<script>alert('xss')</script>` and similar payloads                                                      | Input validation rejects `<`, `>`, and other HTML metacharacters; Blazor auto-encodes rendered output                                                                       |
| **Plaintext Passwords**          | Identified during initial implementation with raw SQLite                                                              | Migrated to ASP.NET Core Identity which hashes passwords with PBKDF2 by default; the registration form displays the stored hash to verify no plaintext is persisted         |
| **Default Auth Scheme Conflict** | `AddIdentity` registered cookie auth as the default scheme, overriding JWT Bearer — all API requests returned 401/403 | Replaced with `AddIdentityCore` + `AddRoles` to avoid hijacking the auth scheme                                                                                             |
| **JWT Claim Type Mismatch**      | Admin users were treated as Employees; `IsInRole("Admin")` always returned false                                      | Set `RoleClaimType = "role"` in `TokenValidationParameters` and `MapInboundClaims = false` to prevent the JWT middleware from remapping short-form claims to long-form URIs |
| **Token Lost on Refresh**        | Page refresh wiped in-memory JWT, redirecting authenticated users to login                                            | Persisted token to `localStorage`; `AppAuthStateProvider` restores it on startup                                                                                            |

Copilot was instrumental in diagnosing these issues — it identified the `AddIdentity` vs `AddIdentityCore` distinction, the `MapInboundClaims` behaviour that silently remapped claim types, and generated the `localStorage` persistence layer.

## Security Tests

Two dedicated test pages validate the application's security:

### Security Tests (`/security-tests`)

Client-side tests that run `InputValidation` against known attack payloads:

- **SQL Injection** — 5 injection patterns (UNION, DROP TABLE, boolean logic bypass, comment injection)
- **XSS** — 5 XSS vectors (script tags, event handlers, javascript: URIs)
- **Password Validation** — Tests that malicious passwords are blocked while valid ones are allowed

### Login & Access Tests (`/auth-tests`)

Live API tests against the authentication endpoints:

- **Invalid Login Attempts** — Wrong password, non-existent user, empty credentials, SQL injection in username, XSS in username
- **Unauthorised Access** — Attempts to register users without a token and without admin privileges
- **Role Verification** — Confirms Admin and Employee roles behave correctly across the UI

## Tech Stack

- .NET 10.0, Blazor WebAssembly, ASP.NET Core Minimal API
- ASP.NET Core Identity with EF Core (SQLite)
- JWT Bearer Authentication
- SafeVaultShared class library for input validation
