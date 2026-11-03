# Navigation and administration

## Where to go

The sidebar now has one **Users** link, alongside **Areas** and **Locations** in an Admin-only Administration group. Locations are the existing SQL `Stations` records; this change does not introduce a second competing location hierarchy. Active navigation follows the real Razor route. Environmental and the other sidebar icons use inline SVG and remain available without the external icon font. The mobile menu supports its toggle, Escape and outside-click dismissal.

Admin can create, list, edit and delete users; manage roles from the same Users page; and create, list, edit, deactivate or delete areas/locations. Public registration links are removed and the old registration URL is Admin-only and forwards to user creation. Account creation does not sign the administrator in as the new account. Normal signed-in users can access operational overviews, including Dashboard, Meter Readings and Waste Readings. Administration, legacy admin routes and network editing remain Admin-only. Operational pages require sign-in; the landing/error pages stay public.

User updates synchronize the employee email. User deletion removes the Identity account but retains its inactive employee record and IDs referenced by reading history. Identity and employee changes share the existing Default database connection and an EF transaction. Duplicate emails are rejected. Self-deletion, self-removal of Admin access and removal of the last Admin are rejected. Role changes invalidate existing cookies on their next validation; the validation interval is zero, so active sessions are checked on each request. This adds a database read per authenticated cookie request.

Areas and locations used by meters cannot be deleted. Deactivation is blocked while active dependants exist. Moving an area between locations is blocked while meters reference it, so meter station metadata does not silently become inconsistent. Area/location duplicate names and missing records are checked in SQL. Active dropdown queries now return the station IDs and exclude inactive rows; Admin lists include inactive records for management. The water network editor still uses the supported Lower/Top diagram stations; creating another location does not automatically extend that diagram’s station layout.

## Display changes

The landing page is a short welcome with sign-in/dashboard actions and three plain descriptions. The sign-in form is similarly compact. The shared palette uses navy navigation with light text, white cards with dark text and blue actions. Native date controls explicitly use a light background and light colour scheme so calendar icons are visible. The layout no longer loads the conflicting duplicated global site/StyleSheet rules; shared structural rules are in app-theme.css, alongside the retained utility component stylesheet.

**Daily average by meter** is on Dashboard and follows its date, meter and department filters. The underlying interval allocator is unchanged; raw readings remain on Meter Readings with a link back to the Dashboard. Flow methodology is available in an expandable explanation, while coverage and missing mapping warnings remain visible. Overview clocks, technical user IDs and concurrency-stamp details are removed. Reading dates and detailed history/audit times are retained where they distinguish actual captures.

## Deployment

This update includes the interval-calculation changes. Apply existing asset/network migrations and `UpgradeIntervalConsumption.sql` as required, followed by **UtilityManagementApp/Scripts/UpgradeAdminReferenceData.sql**, before deploying the app. The admin migration adds the reference-management procedures and corrects active area/location queries. It does not delete existing records and has not been executed against the live database here. The app needs its existing Identity and Employees tables in the same Default database and permission to update them.

The web project now references DataLibrary only, removing the duplicate/out-of-repository SQL project references and the fragile DLL reference into obj. All three projects remain in UtilityMnagement.slnx. Build the web project with `dotnet build UtilityManagerProjects/UtilityManagerProjects.csproj`. Building/publishing the database project still requires the appropriate Windows SSDT tooling.

## Verification

- The actual web project, all C# sources and all Razor views compile; existing member-hiding/nullability warnings remain.
- `dotnet run --project tests/Consumption.Validation/Consumption.Validation.csproj` checks calculation and export agreement.
- `dotnet run --project tests/Admin.Validation/Admin.Validation.csproj` uses a temporary SQLite database to exercise actual Identity services, email synchronization, duplicate rejection, role checks, self/last-Admin protection, audit retention and rollback on an injected employee-write failure. It then exercises actual Razor endpoints for anonymous/non-Admin denial, permitted operational overviews, authorized Admin rendering and antiforgery rejection. The SQLite database is synthetic and does not validate SQL Server stored procedures.
- For browser checks, run that host with `-- --serve`, then use Playwright to run `node tests/app-ui.test.cjs`. Set CHROMIUM_EXECUTABLE_PATH when using a separate Chromium binary. The host binds to 127.0.0.1:5188 and uses a test-only role header; it must not be deployed. Browser checks cover desktop/mobile navigation, Admin link visibility, one active route, the Dashboard table, date-field contrast, sampled visible-text contrast and the simplified landing/sign-in views. External CDN assets are deliberately blocked in this test to check navigation independence; it does not validate chart/PDF rendering.

Real SQL Server migration/transaction/concurrency behaviour and production authentication/data still need testing in the target environment. No production accounts, meters, locations or readings were changed by this work.
