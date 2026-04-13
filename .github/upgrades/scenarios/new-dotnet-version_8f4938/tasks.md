# AppOwnsDataStarterKit .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the AppOwnsDataStarterKit upgrade from .NET 6.0 to .NET 10.0. All 5 projects will be upgraded simultaneously in a single atomic operation, followed by testing, UI modernization, and final commit.

**Progress**: 5/5 tasks complete (100%) ![0%](https://progress-bar.xyz/100)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-04-13 21:01)*
**References**: Plan §2.2 Phase 0

- [✓] (1) Verify .NET 10 SDK installed
- [✓] (2) .NET 10 SDK version meets minimum requirements (**Verify**)

---

### [✓] TASK-002: Atomic framework and dependency upgrade *(Completed: 2026-04-13 21:09)*
**References**: Plan §2.2 Phase 1, Plan §4 (all project sections), Plan §5 Package Update Reference, Plan §6 Breaking Changes Catalog

- [✓] (1) Update TargetFramework to net10.0 in all 5 project files per Plan §4.1-§4.5 (AppOwnsDataShared, AppOwnsDataAdmin, AppOwnsDataWebApi, AppOwnsDataClient, AppOwnsDataReactClient)
- [✓] (2) All project files updated to net10.0 (**Verify**)
- [✓] (3) Update all NuGet package references per Plan §5 Package Update Reference (17 package upgrades including deprecated replacements: Microsoft.Identity.Web 1.x → 3.x, Microsoft.Identity.Web.UI 1.x → 3.x, Microsoft.Identity.Client 4.46.2 → latest 4.x stable)
- [✓] (4) All package references updated (**Verify**)
- [✓] (5) Rewrite SPA integration in AppOwnsDataReactClient per Plan §4.5.3 (remove AddSpaStaticFiles/UseSpa/UseSpaStaticFiles, replace with MapFallbackToFile + UseStaticFiles pattern)
- [✓] (6) SPA API rewrite complete - all removed APIs replaced (**Verify**)
- [✓] (7) Restore all dependencies (dotnet restore AppOwnsDataStarterKit.sln)
- [✓] (8) All dependencies restored successfully (**Verify**)
- [✓] (9) Build solution and fix all compilation errors per Plan §6 Breaking Changes Catalog (focus: System.Uri behavioral changes, UseExceptionHandler route verification, Microsoft.Identity.Web 3.x API updates)
- [✓] (10) Solution builds with 0 errors (**Verify**)

---

### [✓] TASK-003: Run full test suite and validate upgrade *(Completed: 2026-04-13 17:09)*
**References**: Plan §2.2 Phase 2, Plan §8 Testing & Validation Strategy

- [✓] (1) Run all test projects in solution (if present)
- [✓] (2) Fix any test failures (reference Plan §6 Breaking Changes for behavioral changes in System.Uri, UseExceptionHandler, EF Core LINQ translation)
- [✓] (3) Re-run tests after fixes
- [✓] (4) All tests pass with 0 failures OR no test projects present (**Verify**)

---

### [✓] TASK-004: UI Modernization *(Completed: 2026-04-13 21:18)*
**References**: Plan §2.2 Phase 3, Plan §7 UI Modernization Guidance

- [✓] (1) Update Bootstrap 4 to Bootstrap 5 in AppOwnsDataAdmin per Plan §7.1 (update CDN references in _Layout.cshtml, replace thead-dark with table-dark, update data-toggle to data-bs-toggle, update data-dismiss to data-bs-dismiss, replace mr-*/ml-* with me-*/ms-*)
- [✓] (2) Bootstrap 5 updates complete (**Verify**)
- [✓] (3) Update Font Awesome 4 to Font Awesome 6 in AppOwnsDataAdmin per Plan §7.2 (update CDN reference in _Layout.cshtml, replace icon classes: fa-building-o → fa-regular fa-building, fa-user-plus → fa-solid fa-user-plus, fa-bar-chart → fa-solid fa-chart-bar, fa-external-link → fa-solid fa-arrow-up-right-from-square, fa-binoculars → fa-solid fa-binoculars, fa-trash → fa-solid fa-trash)
- [✓] (4) Font Awesome 6 updates complete (**Verify**)
- [✓] (5) Apply Tenants list view improvements per Plan §7.3 (add table-hover and table-responsive classes, convert inline styles to Bootstrap 5 utility classes, update action buttons to btn btn-sm btn-outline-* pattern)
- [✓] (6) UI improvements applied and render correctly (**Verify**)

---

### [✓] TASK-005: Final commit *(Completed: 2026-04-13 21:18)*
**References**: Plan §11 Source Control Strategy

- [✓] (1) Commit all changes with message: "chore: upgrade solution from net6.0 to net10.0\n\n- Update TargetFramework in all 5 projects\n- Upgrade 17 NuGet packages to net10.0-compatible versions\n- Replace deprecated Microsoft.Identity.Web 1.x with 3.x\n- Rewrite AppOwnsDataReactClient SPA integration (removed SPA extensions)\n- Upgrade Bootstrap 4 to Bootstrap 5 in Razor views\n- Upgrade Font Awesome 4 to Font Awesome 6 icon names\n- Fix System.Uri and UseExceptionHandler behavioral changes"

---







