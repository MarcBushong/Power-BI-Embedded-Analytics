# .NET 10 Upgrade Plan — AppOwnsDataStarterKit

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Migration Strategy](#2-migration-strategy)
3. [Detailed Dependency Analysis](#3-detailed-dependency-analysis)
4. [Project-by-Project Migration Plans](#4-project-by-project-migration-plans)
   - [AppOwnsDataShared](#41-appownsdatashared)
   - [AppOwnsDataAdmin](#42-appownsdataadmin)
   - [AppOwnsDataWebApi](#43-appownsdatawebapi)
   - [AppOwnsDataClient](#44-appownsdataclient)
   - [AppOwnsDataReactClient](#45-appownsdatareactclient)
5. [Package Update Reference](#5-package-update-reference)
6. [Breaking Changes Catalog](#6-breaking-changes-catalog)
7. [UI Modernization Guidance](#7-ui-modernization-guidance)
8. [Testing & Validation Strategy](#8-testing--validation-strategy)
9. [Risk Management](#9-risk-management)
10. [Complexity & Effort Assessment](#10-complexity--effort-assessment)
11. [Source Control Strategy](#11-source-control-strategy)
12. [Success Criteria](#12-success-criteria)

---

## 1. Executive Summary

### Scenario Description

Upgrade all 5 projects in `AppOwnsDataStarterKit.sln` from **net6.0** to **net10.0** (LTS). The upgrade is performed on a dedicated branch (`upgrade-to-NET10`) using an **All-At-Once** atomic strategy.

### Scope

| Dimension | Value |
|---|---|
| **Projects** | 5 (all `net6.0` → `net10.0`) |
| **Total LOC** | 3,948 |
| **NuGet Packages** | 24 total — 17 upgrades recommended, 4 deprecated |
| **API Issues** | 5 source-incompatible, 10 behavioral changes |
| **Estimated LOC to Modify** | 15+ lines |
| **Overall Difficulty** | 🟢 Low across all projects |

### Selected Strategy

**All-At-Once Strategy** — All 5 projects upgraded simultaneously in a single atomic operation.

**Rationale**:
- Small solution (5 projects, well within threshold)
- All projects currently on net6.0 — uniform starting point
- Shallow dependency graph (max depth = 2, no circular dependencies)
- All 🟢 Low difficulty ratings from assessment
- No security CVEs — deprecated packages are a maintenance concern, not a blocker
- Clean SDK-style project files throughout

### Complexity Classification

**Simple** — qualifies for fast-batch approach (2–3 detail iterations).

| Indicator | Assessment |
|---|---|
| Projects ≤ 5 | ✅ 5 projects |
| Dependency depth ≤ 2 | ✅ Max depth = 2 |
| No high-risk projects | ✅ All Low |
| No security vulnerabilities | ✅ None detected |
| All packages have clear update path | ✅ All 17 upgradeable packages have suggested versions |

### Critical Items

> ⚠️ **Deprecated Packages** — 4 packages are deprecated and must be replaced (not just version-bumped):
> - `Microsoft.Identity.Web` 1.24.1 / 1.25.2 → upgrade to `3.x`
> - `Microsoft.Identity.Web.UI` 1.24.1 → upgrade to `3.x`
> - `Microsoft.Identity.Client` 4.46.2 → upgrade to latest `4.x` stable

> ⚠️ **SPA Breaking Change** — `AppOwnsDataReactClient` uses removed SPA APIs (`AddSpaStaticFiles`, `UseSpa`). Migration to the modern `app.MapFallbackToFile` + Vite/static serving pattern required.

> 🎨 **UI Modernization** — The `AppOwnsDataAdmin` Razor views use Bootstrap 4 (`thead-dark`) and Font Awesome 4 icon names. Upgrading to Bootstrap 5 and Font Awesome 6 is included in this plan.

---

## 2. Migration Strategy

### Approach: All-At-Once (Atomic Upgrade)

All 5 projects are upgraded simultaneously in a single coordinated operation. No intermediate states exist — the solution moves from `net6.0` to `net10.0` as a unified upgrade.

### Rationale for All-At-Once

- **Unified dependency resolution** — `AppOwnsDataShared` is consumed by both `AppOwnsDataAdmin` and `AppOwnsDataWebApi`. Upgrading all at once avoids a mixed-framework dependency state.
- **Small surface area** — 3,948 LOC total; the blast radius of any issue is manageable.
- **Uniform starting point** — All projects are on identical `net6.0`, so all face the same breaking change categories.
- **No blocking incompatibilities** — 0 binary-incompatible APIs; only 5 source-incompatible (all isolated to `AppOwnsDataReactClient`).

### Execution Order Within the Atomic Upgrade

While all project files are updated simultaneously, the logical resolution order within the build is:

```
[Phase 0] Prerequisites
   └─ Verify .NET 10 SDK installed

[Phase 1] Atomic Upgrade (single pass)
   ├─ 1. Update TargetFramework in all 5 .csproj files
   ├─ 2. Update all NuGet package references
   ├─ 3. Replace deprecated packages with current equivalents
   ├─ 4. Restore dependencies (dotnet restore)
   ├─ 5. Build solution — identify compilation errors
   ├─ 6. Fix all source-incompatible API usages
   └─ 7. Rebuild to verify 0 errors

[Phase 2] Test Validation
   └─ Run all test projects (if present)

[Phase 3] UI Modernization
   ├─ Upgrade Bootstrap 4 → Bootstrap 5 markup in Razor views
   └─ Upgrade Font Awesome 4 → Font Awesome 6 icon names
```

### Dependency-Based Build Resolution Order

Even in an all-at-once upgrade, the compiler resolves in dependency order:

1. **`AppOwnsDataShared`** (leaf — no project dependencies)
2. **`AppOwnsDataAdmin`**, **`AppOwnsDataWebApi`** (depend on Shared)
3. **`AppOwnsDataClient`**, **`AppOwnsDataReactClient`** (standalone — no project dependencies)

---

## 3. Detailed Dependency Analysis

### Dependency Graph

```
AppOwnsDataShared          (ClassLibrary, 0 deps, 2 dependants)
  ├─ consumed by → AppOwnsDataAdmin      (AspNetCore, 1 dep)
  └─ consumed by → AppOwnsDataWebApi     (AspNetCore, 1 dep)

AppOwnsDataClient          (AspNetCore, 0 deps, standalone)
AppOwnsDataReactClient     (AspNetCore, 0 deps, standalone)
```

### Project Profiles

| Project | Kind | Dependencies | Dependants | Files | LOC | Net Change |
|---|---|---|---|---|---|---|
| AppOwnsDataShared | ClassLibrary | 0 | 2 | 8 | 1,034 | Framework + 5 packages |
| AppOwnsDataAdmin | AspNetCore | 1 (Shared) | 0 | 32 | 1,955 | Framework + 5 packages + deprecated replacements |
| AppOwnsDataWebApi | AspNetCore | 1 (Shared) | 0 | 13 | 714 | Framework + 10 packages + deprecated replacements |
| AppOwnsDataClient | AspNetCore | 0 | 0 | 13 | 227 | Framework only |
| AppOwnsDataReactClient | AspNetCore | 0 | 0 | 9 | 18 | Framework + 1 package + SPA API rewrite |

### Critical Path

`AppOwnsDataShared` is on the critical path — both `AppOwnsDataAdmin` and `AppOwnsDataWebApi` consume it. In the atomic upgrade, all project files are updated simultaneously, but if `AppOwnsDataShared` has a package conflict, it will surface as errors in both downstream projects.

### No Circular Dependencies

The dependency graph is a clean DAG (directed acyclic graph). No circular references exist.

---

## 4. Project-by-Project Migration Plans

### 4.1 AppOwnsDataShared

**Current State**: `net6.0` · ClassLibrary · 8 files · 1,034 LOC · Risk: 🟢 Low
**Target State**: `net10.0` · 5 package updates
**Dependencies**: None
**Dependants**: AppOwnsDataAdmin, AppOwnsDataWebApi

#### Migration Steps

1. **Update `TargetFramework`**
   ```xml
   <!-- AppOwnsDataShared\AppOwnsDataShared.csproj -->
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update NuGet Package References**

   | Package | Current | Target |
   |---|---|---|
   | `Microsoft.EntityFrameworkCore` | 6.0.4 | 10.0.5 |
   | `Microsoft.EntityFrameworkCore.SqlServer` | 6.0.4 | 10.0.5 |
   | `Microsoft.EntityFrameworkCore.Tools` | 6.0.4 | 10.0.5 |
   | `Microsoft.Extensions.Configuration.FileExtensions` | 6.0.0 | 10.0.5 |
   | `Microsoft.Extensions.Configuration.Json` | 6.0.0 | 10.0.5 |

3. **Expected Breaking Changes**: None (0 API issues in assessment)

4. **Validation Checklist**
   - [ ] `TargetFramework` updated to `net10.0`
   - [ ] All 5 packages updated
   - [ ] Project builds without errors

---

### 4.2 AppOwnsDataAdmin

**Current State**: `net6.0` · AspNetCore · 32 files · 1,955 LOC · Risk: 🟢 Low
**Target State**: `net10.0` · 5 package updates + 4 deprecated package replacements
**Dependencies**: AppOwnsDataShared

#### Migration Steps

1. **Update `TargetFramework`**
   ```xml
   <!-- AppOwnsDataAdmin\AppOwnsDataAdmin.csproj -->
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update NuGet Package References**

   | Package | Current | Target | Notes |
   |---|---|---|---|
   | `Microsoft.AspNetCore.Authentication.JwtBearer` | 6.0.4 | 10.0.5 | Version align |
   | `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 6.0.4 | 10.0.5 | Version align |
   | `Microsoft.EntityFrameworkCore.Design` | 6.0.4 | 10.0.5 | Version align |
   | `Microsoft.Identity.Web` | 1.24.1 | 3.x (latest stable) | ⚠️ Deprecated — major version upgrade |
   | `Microsoft.Identity.Web.UI` | 1.24.1 | 3.x (latest stable) | ⚠️ Deprecated — major version upgrade |

3. **Deprecated Package Replacement — `Microsoft.Identity.Web` 1.x → 3.x**

   `Microsoft.Identity.Web` 3.x is a major version bump from 1.x with several breaking changes:
   - Namespace `Microsoft.Identity.Web` is preserved; no namespace changes needed.
   - `AddMicrosoftIdentityWebAppAuthentication()` is renamed to `AddMicrosoftIdentityWebApp()` in 2.x+ — verify call sites in `Program.cs` / `Startup.cs`.
   - `ITokenAcquisition` API is largely unchanged but some overloads were removed.
   - Check all `[Authorize]` attributes and policy configurations still function.

4. **Behavioral Changes** (5 instances — test at runtime)
   - `System.Uri` constructor: In .NET 10, `new Uri(relativeString)` with no base throws `UriFormatException` more strictly. Audit all `new Uri(string)` calls in Admin project files; ensure base URIs are provided where relative paths are used.
   - `UseExceptionHandler(string path)` overload: Route-based exception handler behavior changed — error routes now require explicit registration in .NET 10. Verify `app.UseExceptionHandler("/Error")` in `Program.cs` has a corresponding Razor page or endpoint.

5. **Validation Checklist**
   - [ ] `TargetFramework` updated to `net10.0`
   - [ ] All packages updated including `Microsoft.Identity.Web` 3.x
   - [ ] `AddMicrosoftIdentityWebApp()` call verified
   - [ ] Exception handler route verified
   - [ ] Authentication flow tested end-to-end
   - [ ] Project builds without errors

---

### 4.3 AppOwnsDataWebApi

**Current State**: `net6.0` · AspNetCore · 13 files · 714 LOC · Risk: 🟢 Low
**Target State**: `net10.0` · 10 package updates + 2 deprecated package replacements
**Dependencies**: AppOwnsDataShared

#### Migration Steps

1. **Update `TargetFramework`**
   ```xml
   <!-- AppOwnsDataWebApi\AppOwnsDataWebApi.csproj -->
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update NuGet Package References**

   | Package | Current | Target | Notes |
   |---|---|---|---|
   | `Microsoft.AspNetCore.Authentication.JwtBearer` | 6.0.8 | 10.0.5 | Version align |
   | `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 6.0.8 | 10.0.5 | Version align |
   | `Microsoft.EntityFrameworkCore` | 6.0.8 | 10.0.5 | Version align |
   | `Microsoft.EntityFrameworkCore.Sqlite` | 6.0.8 | 10.0.5 | Version align |
   | `Microsoft.EntityFrameworkCore.SqlServer` | 6.0.8 | 10.0.5 | Version align |
   | `Microsoft.EntityFrameworkCore.Tools` | 6.0.8 | 10.0.5 | Version align |
   | `Microsoft.Identity.Web` | 1.25.2 | 3.x (latest stable) | ⚠️ Deprecated — major version upgrade |
   | `Microsoft.Identity.Client` | 4.46.2 | Latest 4.x stable | ⚠️ Deprecated version — update |
   | `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 6.0.8 | 10.0.2 | Dev tool |
   | `Newtonsoft.Json` | 13.0.1 | 13.0.4 | Patch update |

3. **Deprecated Package Notes**
   - `Microsoft.Identity.Client` (MSAL.NET): The package itself is not deprecated; version 4.46.2 is outdated. Update to latest `4.x` stable. API surface is backward-compatible within 4.x.
   - `Microsoft.Identity.Web` 1.x → 3.x: Same breaking changes as noted in AppOwnsDataAdmin §4.2.3.

4. **Entity Framework Core 6 → 10 Migration Notes**
   - Run `dotnet ef migrations add <MigrationName>` after upgrade if schema needs updating.
   - `OnConfiguring` using `UseSqlServer` / `UseSqlite` is unchanged.
   - `IQueryable` lazy evaluation behavior may differ — review any raw SQL or complex LINQ that was working around EF6 quirks.
   - EF Core 10 requires re-scaffolding if using database-first approach.

5. **Behavioral Changes** (4 instances — test at runtime)
   - `System.Uri` constructor: Same guidance as §4.2.4. Audit `new Uri(string)` in WebApi files.
   - `UseExceptionHandler(string)`: Verify exception handler route is registered in `Program.cs`.

6. **Validation Checklist**
   - [ ] `TargetFramework` updated to `net10.0`
   - [ ] All 10 packages updated
   - [ ] EF Core migrations verified (no pending migrations break)
   - [ ] JWT Bearer token validation tested
   - [ ] API endpoints return expected responses
   - [ ] Project builds without errors

---

### 4.4 AppOwnsDataClient

**Current State**: `net6.0` · AspNetCore · 13 files · 227 LOC · Risk: 🟢 Low
**Target State**: `net10.0` · No package updates required
**Dependencies**: None

#### Migration Steps

1. **Update `TargetFramework`**
   ```xml
   <!-- AppOwnsDataClient\AppOwnsDataClient.csproj -->
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **No NuGet Package Updates Required** — Assessment shows 0 package issues.

3. **Behavioral Changes** (1 instance — test at runtime)
   - `System.Uri` constructor: Audit any `new Uri(string)` calls for stricter validation in .NET 10.

4. **Validation Checklist**
   - [ ] `TargetFramework` updated to `net10.0`
   - [ ] Project builds without errors
   - [ ] Client embed functionality verified

---

### 4.5 AppOwnsDataReactClient

**Current State**: `net6.0` · AspNetCore · 9 files · 18 LOC · Risk: 🟡 Medium (SPA API removal)
**Target State**: `net10.0` · 1 package update + SPA API rewrite
**Dependencies**: None

> ⚠️ **This project requires the most code changes.** The legacy `Microsoft.AspNetCore.SpaServices.Extensions` SPA hosting pattern is source-incompatible in .NET 10. The entire SPA wiring in `Program.cs` must be rewritten.

#### Migration Steps

1. **Update `TargetFramework`**
   ```xml
   <!-- AppOwnsDataReactClient\AppOwnsDataReactClient.csproj -->
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update NuGet Package References**

   | Package | Current | Target | Notes |
   |---|---|---|---|
   | `Microsoft.AspNetCore.SpaServices.Extensions` | 6.0.5 | 10.0.5 | Updated but APIs still removed — see step 3 |

3. **Rewrite SPA Integration in `Program.cs`**

   **Remove** the following removed APIs:
   - `services.AddSpaStaticFiles(config => { config.RootPath = "ClientApp/build"; })`
   - `app.UseSpaStaticFiles()`
   - `app.UseSpa(spa => { spa.Options.SourcePath = "ClientApp"; ... })`

   **Replace** with the .NET 10 static file + fallback pattern:
   ```csharp
   // In Program.cs — BEFORE app.Run()

   // Serve React build output as static files
   app.UseStaticFiles(new StaticFileOptions {
       FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "build")),
       RequestPath = ""
   });

   // Fallback to index.html for client-side routing
   app.MapFallbackToFile("index.html", new StaticFileOptions {
       FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "build"))
   });
   ```

   For development with React dev server (hot reload), use `app.UseProxyToSpaDevelopmentServer` replacement via `Microsoft.AspNetCore.SpaProxy`:
   ```xml
   <!-- AppOwnsDataReactClient.csproj -->
   <PackageReference Include="Microsoft.AspNetCore.SpaProxy" Version="10.0.5" />
   ```
   ```csharp
   // launchSettings.json — set SpaProxyServerUrl
   // "SPA_PROXY_SERVER_URL": "http://localhost:3000"
   ```

4. **Validation Checklist**
   - [ ] `TargetFramework` updated to `net10.0`
   - [ ] Old SPA APIs fully removed
   - [ ] New static file / fallback pattern in place
   - [ ] React build served correctly in production mode
   - [ ] Hot reload works in development mode
   - [ ] Project builds without errors

---

## 5. Package Update Reference

### Common Package Updates (Affecting Multiple Projects)

| Package | Current | Target | Projects Affected | Reason |
|---|---|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 6.0.4 / 6.0.8 | **10.0.5** | Admin, WebApi | Framework alignment |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 6.0.4 / 6.0.8 | **10.0.5** | Admin, WebApi | Framework alignment |
| `Microsoft.EntityFrameworkCore` | 6.0.4 / 6.0.8 | **10.0.5** | Shared, WebApi | Framework alignment |
| `Microsoft.EntityFrameworkCore.SqlServer` | 6.0.4 / 6.0.8 | **10.0.5** | Shared, WebApi | Framework alignment |
| `Microsoft.EntityFrameworkCore.Tools` | 6.0.4 / 6.0.8 | **10.0.5** | Shared, WebApi | Framework alignment |
| `Microsoft.Identity.Web` | 1.24.1 / 1.25.2 | **3.x latest stable** | Admin, WebApi | ⚠️ Deprecated — major version required |

### Project-Specific Updates

| Package | Current | Target | Project | Reason |
|---|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | 6.0.4 | **10.0.5** | Admin | Framework alignment |
| `Microsoft.Identity.Web.UI` | 1.24.1 | **3.x latest stable** | Admin | ⚠️ Deprecated — major version required |
| `Microsoft.EntityFrameworkCore.Sqlite` | 6.0.8 | **10.0.5** | WebApi | Framework alignment |
| `Microsoft.Identity.Client` | 4.46.2 | **Latest 4.x stable** | WebApi | ⚠️ Deprecated version |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 6.0.8 | **10.0.2** | WebApi | Dev tooling alignment |
| `Newtonsoft.Json` | 13.0.1 | **13.0.4** | WebApi | Patch — bug fixes |
| `Microsoft.AspNetCore.SpaServices.Extensions` | 6.0.5 | **10.0.5** | ReactClient | Framework alignment |

### No Update Required (Already Compatible)

| Package | Version | Projects | Notes |
|---|---|---|---|
| `Microsoft.PowerBi.Api` | 4.5.0 | Admin | ✅ Compatible with net10.0 |
| `Microsoft.PowerBI.Api` | 4.9.0 | WebApi | ✅ Compatible with net10.0 |
| `Swashbuckle.AspNetCore` | 6.4.0 | WebApi | ✅ Compatible with net10.0 |

### Deprecated Package Upgrade Notes

#### `Microsoft.Identity.Web` 1.x → 3.x
- **Why deprecated**: Microsoft.Identity.Web 1.x targeted .NET 5/6 era APIs now removed.
- **Replacement**: `Microsoft.Identity.Web` 3.x (same package name, major version bump)
- **Breaking changes in 3.x**:
  - `AddMicrosoftIdentityWebAppAuthentication()` → renamed to `AddMicrosoftIdentityWebApp()`
  - Token cache providers API updated — verify `AddDistributedTokenCaches()` / `AddInMemoryTokenCaches()` calls
  - Minimum .NET version enforced at 8.0+
- **Action**: Search all `Program.cs` files for old API names before updating

#### `Microsoft.Identity.Client` 4.46.2 → Latest 4.x
- **Why flagged**: Outdated patch version with known bugs.
- **Replacement**: Same package, latest 4.x stable (e.g., 4.66.x).
- **Breaking changes**: None within 4.x minor/patch updates.

---

## 6. Breaking Changes Catalog

### Source-Incompatible Changes (Require Code Fix)

| API | Project | Category | Required Action |
|---|---|---|---|
| `SpaApplicationBuilderExtensions.UseSpa()` | ReactClient | Source Incompatible | Replace with `MapFallbackToFile` + static files (see §4.5) |
| `SpaStaticFilesOptions.RootPath` | ReactClient | Source Incompatible | Remove — property no longer exists |
| `SpaStaticFilesExtensions.AddSpaStaticFiles()` | ReactClient | Source Incompatible | Remove — use `UseStaticFiles` with `PhysicalFileProvider` |
| `SpaApplicationBuilderExtensions` (type) | ReactClient | Source Incompatible | Entire type removed — all usages must be removed |
| `SpaStaticFilesExtensions` (type) | ReactClient | Source Incompatible | Entire type removed — all usages must be removed |

### Behavioral Changes (Require Runtime Testing)

| API | Projects | Count | Change Description | Mitigation |
|---|---|---|---|---|
| `System.Uri` / `new Uri(string)` | Admin, Client, WebApi | 4 | In .NET 10, relative URIs passed to `new Uri(string)` throw `UriFormatException` more strictly. Previously some edge cases were silently accepted. | Audit all `new Uri(string)` calls. Ensure absolute URIs or use `new Uri(baseUri, relativeUri)` overload. |
| `UseExceptionHandler(string path)` | Admin, WebApi | 2 | Error route must now be a fully registered endpoint or Razor page. Unregistered paths result in a 404 loop instead of a fallback. | Verify that the error path (e.g., `/Error`) has a corresponding `@page` directive or `app.Map()` registration. |

### Entity Framework Core 6 → 10 Behavioral Notes

- **LINQ translation**: EF Core 10 is stricter about client-side evaluation. Queries that were silently evaluated client-side in EF6 will now throw unless `.AsEnumerable()` is explicitly added.
- **Migration history**: Existing migration history table is fully compatible — no re-migration needed.
- **`DateTime` precision**: SQL Server datetime2 precision handling is more consistent. Validate timestamp fields if precise equality checks are used in queries.
- **Owned entity changes**: If `AppOwnsDataShared` uses owned entities, verify configuration syntax is unchanged (EF 10 removed some obsolete fluent API overloads).

---

## 7. UI Modernization Guidance

The `AppOwnsDataAdmin` project uses Razor Pages with Bootstrap 4 and Font Awesome 4 (free tier). As part of this upgrade, the following UI improvements are recommended to align with current library standards and improve customer experience.

### 7.1 Bootstrap 4 → Bootstrap 5

Bootstrap 5 ships with `.NET 10` project templates as the default. Key changes relevant to the existing views:

| Bootstrap 4 | Bootstrap 5 Replacement | Affected Views |
|---|---|---|
| `thead-dark` | `table-dark` (on `<thead>`) | All table views incl. Tenants list |
| `btn-block` | `d-grid` wrapper + `w-100` on button | Action buttons |
| `mr-*` / `ml-*` | `me-*` / `ms-*` (RTL-aware) | Spacing utilities |
| `data-toggle` | `data-bs-toggle` | Dropdowns, modals |
| `data-dismiss` | `data-bs-dismiss` | Modal close buttons |
| jQuery dependency | Removed (vanilla JS) | `_Layout.cshtml` script block |

**CDN reference update in `_Layout.cshtml`**:
```html
<!-- Bootstrap 5 CSS -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
<!-- Bootstrap 5 JS Bundle (includes Popper) -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
```

### 7.2 Font Awesome 4 → Font Awesome 6 (Free)

Font Awesome 6 reorganized icon names and prefixes. Icons used in the Tenants Razor view require the following updates:

| Font Awesome 4 Class | Font Awesome 6 Class | Usage |
|---|---|---|
| `fa fa-building-o` | `fa-regular fa-building` | Page heading |
| `fa fa-user-plus` | `fa-solid fa-user-plus` | Onboard button |
| `fa fa-bar-chart` | `fa-solid fa-chart-bar` | Embed action icon |
| `fa fa-external-link` | `fa-solid fa-arrow-up-right-from-square` | PBI Web action icon |
| `fa fa-binoculars` | `fa-solid fa-binoculars` | View Details icon |
| `fa fa-trash` | `fa-solid fa-trash` | Delete icon |

**CDN reference update in `_Layout.cshtml`**:
```html
<!-- Font Awesome 6 Free -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.6.0/css/all.min.css" />
```

### 7.3 Tenants List View — Specific Improvements

For the Customers/Tenants Razor page (`Tenants.cshtml` or equivalent), apply these improvements:

**Current markup (Bootstrap 4 + FA4)**:
```html
<thead class="thead-dark">
  ...
  <i class="fa fa-bar-chart"></i>
  <i class="fa fa-external-link"></i>
```

**Updated markup (Bootstrap 5 + FA6)**:
```html
<thead>
  <tr class="table-dark">
  ...
  <i class="fa-solid fa-chart-bar"></i>
  <i class="fa-solid fa-arrow-up-right-from-square"></i>
```

**Additional Bootstrap 5 table improvements**:
- Add `table-hover` class for row highlight on mouse-over
- Add `table-responsive` wrapper div for mobile viewport support
- Replace inline `style="text-align:center"` with Bootstrap 5 `text-center` utility class

```html
<!-- Improved table wrapper -->
<div class="table-responsive">
  <table class="table table-bordered table-striped table-hover">
    <thead>
      <tr class="table-dark">
        <th>Customer</th>
        <th>Created</th>
        <th>Workspace ID</th>
        <th class="text-center">Embed</th>
        <th class="text-center">PBI Web</th>
        <th class="text-center">View</th>
        <th class="text-center">Delete</th>
      </tr>
    </thead>
    @foreach (var tenant in Model) {
      <tr>
        <td>@tenant.Name</td>
        <td>@tenant.Created.ToString("yyyy-MM-dd 'at' hh:mm tt")</td>
        <td><code>@tenant.WorkspaceId</code></td>
        <td class="text-center"><a href="~/Home/Embed/?TenantName=@tenant.Name" class="btn btn-sm btn-outline-primary" target="_blank" title="Embed"><i class="fa-solid fa-chart-bar"></i></a></td>
        <td class="text-center"><a href="@tenant.WorkspaceUrl" class="btn btn-sm btn-outline-secondary" target="_blank" title="Open in Power BI"><i class="fa-solid fa-arrow-up-right-from-square"></i></a></td>
        <td class="text-center"><a href="Tenant/?Name=@tenant.Name" class="btn btn-sm btn-outline-info" title="View Details"><i class="fa-solid fa-binoculars"></i></a></td>
        <td class="text-center"><a href="DeleteTenant/?TenantName=@tenant.Name" class="btn btn-sm btn-outline-danger" title="Delete"><i class="fa-solid fa-trash"></i></a></td>
      </tr>
    }
  </table>
</div>
```

**Key UX improvements in the updated markup**:
- Workspace ID rendered in `<code>` tag for monospace readability
- Action icons promoted to `btn btn-sm btn-outline-*` — clearly interactive, color-coded by action type
- `table-hover` adds subtle row highlighting for easier scanning
- `table-responsive` wrapper prevents horizontal overflow on narrow screens
- `text-center` replaces all inline `style` attributes

---

## 8. Testing & Validation Strategy

### Build Validation

After the atomic upgrade, the following build verification must pass before any testing:

```
dotnet restore AppOwnsDataStarterKit.sln
dotnet build AppOwnsDataStarterKit.sln --no-incremental
```

Expected outcome: **0 errors, 0 warnings** (warnings become errors in production builds).

### Per-Project Validation Checklist

| Project | Build | URI Audit | Auth Flow | API Endpoints | SPA Served | UI Renders |
|---|---|---|---|---|---|---|
| AppOwnsDataShared | ✅ required | — | — | — | — | — |
| AppOwnsDataAdmin | ✅ required | ✅ required | ✅ required | — | — | ✅ required |
| AppOwnsDataWebApi | ✅ required | ✅ required | ✅ required | ✅ required | — | — |
| AppOwnsDataClient | ✅ required | ✅ required | — | — | — | ✅ required |
| AppOwnsDataReactClient | ✅ required | — | — | — | ✅ required | — |

### Functional Validation Areas

#### Authentication (Admin + WebApi)
- OIDC sign-in flow using `Microsoft.Identity.Web` 3.x
- JWT Bearer token validation on protected API endpoints
- Token acquisition and refresh cycle

#### Power BI Embed (Admin + Client)
- Workspace list loads from Power BI API
- Embed token generated and passed to front end
- Report renders in embed container

#### Entity Framework (Shared + WebApi)
- `dotnet ef database update` applies pending migrations cleanly
- CRUD operations on tenant entities function correctly
- SQLite (dev) and SQL Server (prod) connections verified

#### React SPA (ReactClient)
- Production build (`npm run build`) produces output in `ClientApp/build`
- `index.html` served on all non-API routes
- API calls from React front end reach WebApi endpoints

#### UI Modernization (Admin)
- Bootstrap 5 styles render correctly (no visual regressions)
- Font Awesome 6 icons display on all pages
- Responsive table renders on mobile viewport (< 768px)
- Row hover highlight works on Customers table

---

## 9. Risk Management

### Risk Register

| Project | Risk | Level | Description | Mitigation |
|---|---|---|---|---|
| AppOwnsDataReactClient | SPA API removal | 🟡 Medium | `AddSpaStaticFiles` / `UseSpa` removed in .NET 10 — all SPA wiring must be rewritten | Follow exact pattern in §4.5; test production build before merge |
| AppOwnsDataAdmin / WebApi | `Microsoft.Identity.Web` 1.x → 3.x | 🟡 Medium | Major version bump with renamed APIs; auth flow could silently break | Verify `AddMicrosoftIdentityWebApp()` rename; test full OIDC/JWT flow end-to-end |
| AppOwnsDataWebApi | EF Core 6 → 10 LINQ changes | 🟢 Low | Client-side evaluation queries now throw; edge-case query changes | Run full suite of data access scenarios; inspect compiler warnings for EF |
| All | `System.Uri` behavioral change | 🟢 Low | Stricter URI validation; relative URIs may throw at runtime | Search all projects for `new Uri(` and verify each call site |
| AppOwnsDataAdmin / WebApi | `UseExceptionHandler(string)` | 🟢 Low | Error route must be a registered endpoint | Verify error page route is registered as Razor page or `app.Map()` |
| AppOwnsDataAdmin | Bootstrap 4 → 5 | 🟢 Low | CSS class renames could cause visual regressions | Test all Razor pages in browser after Bootstrap 5 update |

### Contingency Plans

#### If `Microsoft.Identity.Web` 3.x Breaks Authentication
- Temporarily pin to `Microsoft.Identity.Web` 2.x as an intermediate step
- Consult Microsoft Identity Platform migration guide: https://github.com/AzureAD/microsoft-identity-web/wiki/1.x-2.x-Changelog
- Ensure Azure App Registration is not using deprecated API permissions

#### If SPA Rewrite Blocks Build
- Temporarily comment out the React SPA project from the solution while other projects are validated
- Reintegrate after SPA pattern is confirmed working in isolation

#### Rollback Strategy
- All changes are isolated to branch `upgrade-to-NET10`
- Roll back by checking out `main` — no changes have been made to the source branch
- Single commit approach means rollback is a single `git revert` or branch delete

---

## 10. Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | LOC | Package Changes | API Issues | Key Drivers |
|---|---|---|---|---|---|
| AppOwnsDataShared | 🟢 Low | 1,034 | 5 updates | 0 | Pure framework + package bump |
| AppOwnsDataAdmin | 🟡 Medium | 1,955 | 5 updates + 2 deprecated replacements | 5 behavioral | `Microsoft.Identity.Web` major version + Bootstrap/FA upgrade |
| AppOwnsDataWebApi | 🟡 Medium | 714 | 10 updates + 2 deprecated replacements | 4 behavioral | Most packages, EF Core upgrade, MSAL update |
| AppOwnsDataClient | 🟢 Low | 227 | 0 | 1 behavioral | Minimal changes — framework bump only |
| AppOwnsDataReactClient | 🟡 Medium | 18 | 1 update | 5 source incompatible | Small file count but SPA API rewrite required |

### Solution-Level Assessment

| Dimension | Rating | Notes |
|---|---|---|
| Overall complexity | 🟢 Low-Medium | No high-risk projects; ReactClient is the main point of friction |
| Dependency complexity | 🟢 Low | Clean 2-level DAG, no circular deps |
| Package complexity | 🟡 Medium | 4 deprecated packages require non-trivial replacement work |
| API compatibility | 🟢 Low | 0 binary incompatible; 5 source incompatible all in one project |
| UI modernization | 🟢 Low | Mechanical class renames; visual verification needed |

---

## 11. Source Control Strategy

### Branching

| Branch | Purpose |
|---|---|
| `main` | Source branch — untouched during upgrade |
| `upgrade-to-NET10` | All upgrade changes land here (already created and active) |

### Commit Strategy

Use a **single commit** approach for the entire upgrade, consistent with the All-At-Once strategy:

```
git add .
git commit -m "chore: upgrade solution from net6.0 to net10.0

- Update TargetFramework in all 5 projects
- Upgrade 17 NuGet packages to net10.0-compatible versions
- Replace deprecated Microsoft.Identity.Web 1.x with 3.x
- Rewrite AppOwnsDataReactClient SPA integration (removed SPA extensions)
- Upgrade Bootstrap 4 to Bootstrap 5 in Razor views
- Upgrade Font Awesome 4 to Font Awesome 6 icon names
- Fix System.Uri and UseExceptionHandler behavioral changes"
```

If the upgrade requires multiple work sessions, intermediate `WIP:` prefixed commits on `upgrade-to-NET10` are acceptable, to be squashed before merging to `main`.

### Merge Process

1. All validation checklist items in §8 must pass
2. Open a Pull Request: `upgrade-to-NET10` → `main`
3. PR description should reference this plan and link `assessment.md`
4. Review diff for any unintended changes outside the upgrade scope
5. Merge using **squash merge** to keep `main` history clean

---

## 12. Success Criteria

### Technical Criteria

- [ ] All 5 projects target `net10.0`
- [ ] All 17 package upgrades applied to their suggested versions
- [ ] All 4 deprecated packages replaced with supported equivalents
- [ ] `dotnet build AppOwnsDataStarterKit.sln` completes with **0 errors, 0 warnings**
- [ ] `dotnet restore` completes with no unresolved dependencies
- [ ] `System.Uri` call sites audited across Admin, Client, WebApi
- [ ] `UseExceptionHandler` error route verified in Admin and WebApi
- [ ] `AppOwnsDataReactClient` SPA APIs fully replaced with `.NET 10` pattern
- [ ] React app served correctly in production and development modes
- [ ] EF Core migrations apply cleanly (`dotnet ef database update`)

### Authentication Criteria

- [ ] `Microsoft.Identity.Web` 3.x installed and `AddMicrosoftIdentityWebApp()` API used
- [ ] OIDC sign-in flow works end-to-end in Admin
- [ ] JWT Bearer token validation works in WebApi
- [ ] Token acquisition for Power BI API succeeds

### UI Modernization Criteria

- [ ] Bootstrap 5 CDN reference in `_Layout.cshtml`
- [ ] Font Awesome 6 CDN reference in `_Layout.cshtml`
- [ ] All `thead-dark` replaced with `table-dark`
- [ ] All Font Awesome 4 icon classes updated to Font Awesome 6 equivalents
- [ ] Tenants table renders responsively on mobile viewport
- [ ] Action icon buttons are visually distinct and color-coded

### Quality Criteria

- [ ] No inline `style` attributes remaining (replaced by Bootstrap 5 utility classes)
- [ ] All changes committed to `upgrade-to-NET10` branch
- [ ] Pull Request opened against `main` with this plan referenced

### Process Criteria

- [ ] All-At-Once strategy followed — single atomic upgrade, no intermediate states on `main`
- [ ] Source branch (`main`) untouched throughout upgrade
- [ ] Plan document (`plan.md`) retained in `.github/upgrades/scenarios/` for traceability
