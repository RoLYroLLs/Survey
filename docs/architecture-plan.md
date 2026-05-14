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

- Survey authoring follows `Survey -> Version -> Section -> Question -> Option`.
- Once a survey version has an assignment, that version becomes locked for editing.
- Public survey links identify a single assignment and can expire.
- Staff can submit on behalf of a respondent, and the saved response records both the acting employee context and the final contact snapshot used at submission time.
- Survey responses support yes/no, single-choice, multi-select, and long-text answers.

## Infrastructure Choices

- Target framework is `net10.0`.
- UI uses Blazor Web App with Interactive Server rendering.
- Identity is stored in the same application database as survey data.
- Database provider is selected per deployment with `Database:Provider` and `ConnectionStrings:Default`.
- `Sqlite` is the default provider for local development and lightweight deployments.
- `SqlServer` is supported through a separate migration assembly for production-style deployments.

## Deployment Notes

- The web host is structured for Azure hosting and Aspire-managed local orchestration.
- Production should use a durable shared database and external secret storage.
- Seed-admin settings are read from configuration so the first administrator can be bootstrapped without self-registration.
