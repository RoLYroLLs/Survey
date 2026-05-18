# Survey Architecture Plan

## Solution Shape

- `src/Survey.AppHost`: Aspire entry point for local orchestration and future Azure-hosted composition.
- `src/Survey.ServiceDefaults`: shared Aspire and service-default wiring.
- `src/Survey.Domain`: core entities and business rules for surveys, assignments, responses, and answer types.
- `src/Survey.Application`: UI-facing models and service contracts for admin and public survey flows.
- `src/Survey.Infrastructure`: EF Core persistence, Identity integration, database-provider selection, and application service implementation.
- `src/Survey.Migrations.SqlServer`: SQL Server migration assembly.
- `src/Survey.Migrations.Sqlite`: SQLite migration assembly.
- `src/Survey.Web`: Blazor Web App host for the admin experience and public recipient experience.
- `tests/*`: focused test projects for domain, application, infrastructure, and web-adjacent validation.

## Functional Boundaries

- The platform now distinguishes between platform administration and tenant operations; platform/global administration lives under `/admin`, while tenant-facing operations live under `/app`.
- Tenant-facing pages now live physically under `Components/Pages/App`, while platform/global pages live under `Components/Pages/Admin`.
- Legacy tenant `/admin` route aliases have been removed as part of the hard cut; tenant workflows should be reachable only through `/app`.
- Survey authoring follows `Survey -> Version -> Section -> Question -> Option`.
- Once a survey version has an assignment, that version becomes locked for editing.
- Public survey links identify a single assignment and can expire.
- Assignments support archival in addition to active/completed/expired lifecycle states.
- Archived assignments remain available for reference in admin views but are not fillable through public or staff survey flows.
- Staff can submit on behalf of a respondent, and the saved response records both the acting employee context and the final contact snapshot used at submission time.
- Survey responses support yes/no, single-choice, multi-select, and long-text answers.

## Multi-Tenant Security Model

- Multi-tenancy is treated as a backend security boundary, not a UI-only grouping.
- Every tenant-owned record resolves against a server-side active tenant context; client-supplied tenant identifiers must not be trusted for reads or writes.
- Users authenticate globally through `ApplicationUser`, then access tenants through `TenantMembership`.
- Tenant membership roles are `Owner`, `Admin`, and `User`.
- Tenant access is permission-based. Roles provide defaults, and membership-level allow/deny overrides fine-tune effective permissions.
- Platform access is separate from tenant access. Platform administration uses platform permissions and must not imply automatic tenant access.
- A user may belong to multiple tenants. One active tenant membership is selected server-side for the current session, and the UI provides a tenant switcher when needed.
- Self-serve onboarding is phased: first collect email, username, and password; then require email confirmation; then collect name, address, and phone; then create the first tenant. The first membership becomes the initial `Owner`.
- Self-serve onboarding should prefill the first tenant name as `My First Tenant` until the user changes it.
- Tenant admins can invite users into their tenant with secure invitation tokens. Existing accounts can accept an invite after signing in, and new accounts can be created directly from the invitation flow.
- Tenant membership management must prevent privilege escalation, disallow changing one’s own tenant role/status/permissions through normal tenant-admin flows, and block removal, disablement, or demotion of the final enabled `Owner` or the final enabled admin-capable membership.
- Sensitive authorization denials and tenant-admin security changes should be audit logged.

## Contact And Location Model

- A `Person` may own many `Location` records.
- There is no primary location.
- A person record does not require a first name or last name; records may be saved without either name when only contact/location data is available.
- A `Person` supports archival in addition to active state; archived people should be hidden from normal list/select flows by default but remain available for reference and restoration.
- A `Location` uses a nickname so staff can identify the correct household or destination during assignment.
- A `Location` contains both a physical address and a mailing address.
- A `Location` contains multiple phone numbers and multiple email addresses, each with a controlled type or label selected from standard dropdown values instead of free text.
- Person and location phone types use standard values: `Mobile`, `Home`, `Work`, `Fax`, and `Other`.
- Person and location email types use standard values: `Home`, `Work`, and `Other`.
- Person and location contact collections designate one phone and one email as `Primary`; list editing should enforce a single primary selection per collection instead of exposing manual sort ordering.
- A `Person` also keeps profile-level physical address, mailing address, phone numbers, and email addresses.
- When adding or editing a person or location, the required contact shape is: a physical address plus at least one contact method (`Phone` or `Email`).
- Person mailing address is required and must be entered explicitly; it should not silently fall back to the main physical address during save.
- Location and survey mailing addresses may still use explicit copy helpers for convenience, but those are user actions rather than implicit fallback rules.
- A `Person` stores a preferred contact window and a preferred contact method.
- Preferred contact window uses controlled values: `Morning`, `Afternoon`, and `Evening`.
- Preferred contact method uses controlled values: `Call`, `Text`, `Email`, and `Mail`.
- Copy helpers such as `Same as profile address` and `Same as mailing address` are one-time prefill actions, not ongoing synchronization rules.
- Every `SurveyAssignment` belongs to one `Location`.
- Every `SurveyAssignment` must choose at least one reachable contact method from the location (`Phone` or `Email`) so delivery can occur by text or email.
- Assignment contact selection is asymmetric by design: some assignments may have phone only, some email only, and some both.
- Survey prefilling uses the person name, preferred-contact preferences, plus the assigned location contact data that exists for that assignment.
- Survey contact confirmation and response snapshots also support the same `phone-or-email` rule instead of requiring both methods.

## Migration Strategy

- Existing people receive one generated location named `Imported Location`.
- Existing assignments move from direct person ownership to the generated imported location for that person.
- Existing profile contact data remains on the person as profile-level contact data.
- Existing free-text best-time and contact-type values normalize into the closest supported dropdown value during save/update flows, while unchanged historical snapshots remain readable.
- Existing response records stay readable and keep their saved contact snapshots.
- Schema migrations must preserve older assignments while allowing nullable assignment phone/email references and the new assignment archive flag.
- Existing single-tenant tenant-owned data must migrate into one imported default tenant so legacy records stay usable after tenant scoping is enforced.
- Existing administrators should become platform-capable users and owner/admin members of the imported tenant; legacy staff accounts should become tenant members with safe default permissions.

## Infrastructure Choices

- Target framework is `net10.0`.
- UI uses Blazor Web App with Interactive Server rendering.
- Identity is stored in the same application database as survey data.
- Database provider is selected per deployment with `Database:Provider` and `ConnectionStrings:Default`.
- `Sqlite` is the default provider for local development and lightweight deployments.
- `SqlServer` is supported through a separate migration assembly for production-style deployments.
- Tenant-owned entities use server-enforced tenant scoping in the EF Core layer through tenant context, query filtering, and save-time tenant stamping/cross-tenant write rejection.
- Multi-tenant schema support adds `Tenant`, `TenantMembership`, `TenantInvitation`, `TenantSetting`, `PlatformUserPermission`, and `AuditLog` as first-class persistence types.
- SQLite startup repair remains part of the deployment strategy so older local databases can be brought forward safely when schema drift exists.
- Expensive reference-data seeding should use version-tracked seed state instead of replaying the full seed pipeline on every boot.
- Seed routines may be forced manually by configuration for one-off rebuilds, but normal reseeding should happen by incrementing the seed version for the affected routine.

## Deployment Notes

- The web host is structured for Azure hosting and Aspire-managed local orchestration.
- Production should use a durable shared database and external secret storage.
- Seed-admin settings are read from configuration so the first administrator can be bootstrapped without self-registration.
- Tenant bootstrap should remain self-serve, while invitation delivery can use copied secure links until outbound email delivery is introduced.
- Platform administration now exposes dedicated `/admin/users`, `/admin/tenants`, and `/admin/audit` surfaces for platform-user management, tenant oversight, and audit review.
- Application service dependencies should stay split between `ITenantAdministrationService` and `IPlatformAdministrationService`; callers should not depend on a combined admin service contract.

## UX Notes

- Save actions on edit screens should provide immediate, visible confirmation instead of leaving the user on an unchanged-looking form.
- The preferred confirmation pattern is a toast-style notification that appears near the top edge of the screen, can be dismissed manually, and may also auto-dismiss after a short delay.
- Toast notifications should be implemented through one shared app-wide host and service so action feedback uses a single system instead of per-page custom markup.
- Toast visual treatment should communicate severity consistently: success uses green, warning uses yellow, and error uses red.
- The shared toast host should support multiple simultaneous toasts, stack them oldest-first near the top edge of the screen, and animate remaining toasts upward when one is dismissed or expires.
- The authenticated app shell should use a shared top bar across `/app` and `/admin`, with `Survey` branding on the left, a centered tenant-wide search entry point, and tenant/profile controls on the right.
- The tenant-wide top-bar search should search across the current tenant's accessible records and return only sections the current membership is authorized to view.
- The authenticated shell should expose tenant switching as a top-bar control rather than a standalone navigation-only action.
- User profile entry in the shell should render as a colored circular avatar with the user's initial; avatar color is assigned once at account creation and reused until a future profile editor changes it.
- Sidebar menu icons should use a consistent outlined icon style rather than colored emoji-style glyphs.
- Standalone routed list pages should use progressive server-side paging rather than loading the full dataset at once.
- The standard list batch size is `10` by default, with supported view-count options of `10`, `25`, `50`, and `100`.
- Standalone routed list pages should use a `Load More` pattern that appends the next batch beneath the current rows instead of replacing the view with discrete previous/next pages.
- The shared list pager should show `Showing X of Y`, disable `Load More` when there are no more records, and preserve the selected `pageSize` in the query string when filters or searches rebuild the URL.
- Filter, archive, and search changes on standalone routed list pages should reset the loaded batch back to the first page while preserving the selected `pageSize`.
- Standalone routed list pages should allow sorting on every visible data column, remember the last three selected sort columns, and use the most recently selected column as the primary sort.
- Selecting the current primary sort column repeatedly should cycle it through ascending, descending, and removed states while retaining any remaining secondary and tertiary sort columns.
- Embedded child tables inside detail pages are excluded from the progressive paging standard unless they are explicitly upgraded later.
- Person-edit address section titles should use concise end-user labels such as `Address` and `Mailing Address` instead of internal profile-oriented wording.
- Required fields on forms should show a consistent red asterisk next to the field label, with markers aligned to the actual validation rules rather than decorative-only hints.
- Person mailing address is required in the admin editor and must be entered or explicitly copied by the user; it should not silently inherit the physical address during save.
- Assignment list views should support query-string-driven filtering so archived scope, person scope, and status scope survive view/edit/back navigation.
- Assignment status filtering should support `All`, `Active`, `Completed`, and `Expired` in both the global assignments list and the person-specific assignments list.
- Reference-data and workflow dropdowns should use a shared searchable combobox pattern instead of plain browser selects when the option list is long.
- Searchable dropdown behavior should be case-insensitive, filter as the user types, support keyboard navigation, allow `Enter` to select without accidentally submitting the surrounding form, and treat `Tab` / `Shift+Tab` as "select highlighted option, then move focus onward" when the menu is open.
- Forward `Tab` on a focused dropdown should preserve the current selection unless the user has actually started interacting with the option list by typing or using arrow navigation; simple tabbing through a form must not clear or replace existing dropdown values.
- Shared searchable dropdowns should show a single chevron indicator only; component markup and Bootstrap/select styling must not combine to render duplicate chevrons.
- Searchable dropdown menus should render as floating overlays, preserve the trigger width as a minimum, grow only as needed for visible content, track the trigger on scroll and resize, and flip above the field when there is not enough viewport space below.
- Tenant administration should expose a `/app/users` flow for viewing users, creating invitation links, changing tenant role/status, reviewing effective permissions, and editing membership-level permission overrides.
- Tenant administration now also exposes `/app/users/invitations` for reviewing pending/history invitation links and reissuing or revoking them safely inside the current tenant.
- Invitation acceptance should support both existing users and newly created users, with the invited tenant becoming the active tenant after acceptance.
- The tenant navigation should expose a visible `Platform Admin` entry point for users who also hold platform access, so platform-capable users do not have to discover `/admin` manually.
- Tenant owners should have an `/app/geography` settings surface where they can choose which countries, states / territories, and counties their tenant can see and use.
- Tenant geography visibility must be backend-enforced, not UI-only. Address dropdowns, area county assignment, ZIP-to-county inference, and other tenant-facing geography lookups must all respect the owner-managed visibility scope.
- Tenant theme management is tenant-scoped and owner-only; non-owner memberships should neither see the theme action in `/app` navigation nor pass backend theme-update authorization.
- Tenant pages may still deep-link into platform maintenance tools where no tenant-safe equivalent exists yet, but those controls should only be shown to users who also hold the required platform permission.
- Platform reference pages that surface tenant-owned records must only offer tenant navigation when the operator's active tenant context matches the referenced tenant; otherwise, cross-tenant navigation should stay blocked in the UI and the backend.

## Geography Seed Data

- The platform should seed the full United States county list, including county equivalents such as the District of Columbia, Alaska boroughs / census areas, independent cities, and Louisiana parishes.
- Florida ZIP-to-county seed data remains a separate reference dataset and should continue to load after the full county reference list is in place.
