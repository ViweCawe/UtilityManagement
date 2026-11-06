# Counter units, replacements and historical imports

Water counters can be entered in L, m³ or kL. 1 m³ = 1 kL = 1,000 L. Electricity remains kWh. m² is displayed as a square-unit example but is disabled for these utilities because area cannot be converted into water volume. Original counter values use decimal(19,6), with up to six fractional digits. Reports, storage balances and daily averages convert water differences to litres; raw captures retain their original counter unit. No existing counter is multiplied or rewritten by the upgrade.

## Recording a replacement

1. Confirm the old physical meter’s identity, counter unit, direction and placement. If registering historic meters from scratch, create the old meter first with the unit printed on that counter. Confirm a legacy direction through Edit. A later direction correction applies consistently to the replacement chain; it does not change counter values. Its unit cannot be changed once readings exist.
2. Add a new meter at the same measurement boundary. Choose **Replaces an existing physical meter**, enter the cutover date/time, the new serial number and its counter unit. Keep the utility, asset kind, role, direction, area, department and station the same. A changed measurement boundary is a new point, not a replacement.
3. Saving atomically retires the previous meter and links its successor. The previous meter cannot have a second successor or readings after cutover. The old final counter and new starting counter are separate captures; a replacement’s first counter is its baseline. Missing final/baseline captures leave gaps rather than invented usage.
4. Register older captures through Import using each physical meter’s ID. Normal reading entry uses active meters. The register shows active and inactive/replaced tables, with previous/successor links. Open **History / trend** from either meter to inspect the whole chain.

Network links follow the successor. The source point, storage counter references and reporting IDs resolve the verified replacement chain. A network document revision is advanced on replacement so a stale editor cannot overwrite the cutover assignment. Reports, Dashboard, Environmental, Readings and station/storage calculations share normalized intervals. Intervals are calculated separately for each physical counter before joining a measurement-point trend. There is no subtraction between the old meter’s final number and the new meter’s starting number. An inactive meter with no successor remains available in its dedicated history view; it is not silently treated as an active site supply.

The history view defaults to five years and supports up to ten years, with monthly covered-day subtotals and a daily-average curve. It does not connect missing intervals or reconstruct data that was deleted. Current network topology and classifications apply to historical reporting; historical network rewiring is not modelled by replacement links. Raw reading filters select physical counters, while a selected measurement point’s KPI/trend can span its replacements.

## Spreadsheet import

Admins can upload CSV or XLSX (first sheet, 5 MB compressed / 30 MB expanded maximum, up to 10,000 rows). Download the template from the page:

| MeterId | ReadingDate | Counter | Unit |
|---|---|---:|---|
| 12 | 2021-01-08 | 203.750 | m³ |
| 35 | 2026-11-05T10:30:00 | 500 | L |

These are examples, not imported site data. Use actual capture dates, ISO date/time text or real Excel date cells, a plain decimal point and the original unit. A wide legacy workbook must be reshaped to one row per actual counter capture and physical meter ID. Weekly date ranges need a confirmed capture date; formulas, usage totals and carried-forward blanks cannot be imported as physical readings. The app does not guess old meter identities, workbook multipliers or replacement dates.

Preview checks every row for unit agreement, precision, service dates, duplicates, existing conflicts and counter decreases. Retired meters are allowed only within recorded service dates. Identical saved captures are skipped; conflicting captures block the batch instead of overwriting them. A signed, user-bound preview expires after 15 minutes. Confirmation revalidates against current data, then the SQL transaction locks affected meters and applies the whole batch. Imported capture details identify the source file and batch; the audit ledger stores filename, SHA-256, employee, timestamp and inserted count. No spreadsheet was imported as part of this code change.

## Deployment

Apply `UtilityManagementApp/Scripts/UpgradeMeterLifecycle.sql` to the intended SQL Server database **before** deploying the web build. It follows the existing meter-assets/network/interval upgrades. Run with a client that stops on errors (for example `sqlcmd -b -S SERVER -d DATABASE -i UtilityManagementApp/Scripts/UpgradeMeterLifecycle.sql`). It adds replacement metadata, the import audit ledger and fractional counter support, and updates the meter/readings procedures and interval view. Definitions are also included in the database project. The script is idempotent for this repository’s schema; custom dependent indexes/constraints need deployment review. Existing units must be verified against the original counters before importing historic data.

No production database was accessed or changed. Validation includes application builds, a 2021 replacement chain using m³ and L, source/station agreement, CSV/XLSX and duplicate/service-date tests, SQL Server syntax parsing and migration-definition parity, and actual Razor browser checks with synthetic data. SQL syntax checks do not replace running the migration and import/replacement transactions on a SQL Server test copy; that runtime deployment check remains outstanding.

## Direction save correction

If the lifecycle upgrade has already been applied, run `Scripts/UpgradeMeterDirectionSave.sql` (inside `UtilityManagementApp`) before deploying the save correction. The full lifecycle script includes the same corrected procedure for new upgrades. Direction is an editable classification; correcting it updates the measurement point’s predecessor/successor chain atomically and advances the network revision. Unit, placement and physical service-date protections remain.

Serial number is optional on create/edit. Validation errors, including field-level errors, are shown visibly. After saving, the app reads the meter back and compares the editable fields before showing a green **Saved successfully** message on the edit page. Ignored or unconfirmed writes show an error and never show success. Outdated database procedures produce a database-update explanation rather than a misleading replacement warning.
