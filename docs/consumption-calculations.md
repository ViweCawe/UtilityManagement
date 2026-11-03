# Water flow and daily estimates

## What changed

Weekly capture is valid. A cumulative meter counter is not period consumption. Adding Main Meter, Garage Roof and their downstream tanks or outlets counts the same water repeatedly. Putting an entire weekly difference on its closing capture date also produces misleading spikes and empty days. The old first-capture-from-zero convention could count the meter’s lifetime counter as new usage; later edits could leave stored differences stale.

Dashboard, Reports, Environmental, Usage, Analytics and Readings summaries now use the same interval allocation and reporting boundaries. Raw captures remain available separately.

## Daily calculation

For consecutive captures on different calendar dates:

`daily estimated flow = (closing counter - opening counter) / elapsed calendar days`

A baseline of 1,000 L on January 1 and 1,700 L on January 8 allocates 100 L to each day January 2–8. A later weekly change of 1,400 L allocates 200 L/day for that next interval. The graph changes when the interval average changes; it cannot recover real daily fluctuations from weekly readings.

Only dates inside the selected period are totalled. Supporting captures outside that period are used when they bracket its dates. The Dashboard shows saved capture counts, the latest per-meter counters and availability explanations even when usage is not ready. The first capture is a baseline, and dates before that baseline or after the last capture remain unknown, not zero. A true unchanged counter supplies covered zero-flow days. Decreasing-counter intervals are excluded with a warning. Correct incorrect captures or create a new meter record for a replacement/reset; never silently treat a negative difference as consumption.

The last capture on each calendar day defines that day’s counter for this daily estimate. All intraday captures remain in history. This is a calendar-day convention, not an hourly flow reconstruction. Decimal allocation preserves each complete interval’s total, including fractional daily averages.

## Reporting boundaries

| Point | Site total treatment |
| --- | --- |
| Main Meter | External municipal inflow |
| Fire Hydrant | Separate metered municipal inflow, outside Main Meter |
| Borehole Incoming | Metered drilled-water inlet to storage |
| Garage Roof, Borehole Out, downstream tanks and outlets | Meter-level detail; no additional site supply |

Use the network editor’s **External supply** checkbox for future external Source or Storage points, and assign each boundary its actual active water meter. Existing saved maps without this field retain the three known boundary identities until edited. A disconnected tank is not automatically a new supply. Missing source assignments or duplicate mappings make the site aggregate unavailable. Only days covered by every expected boundary meter contribute to a site total and daily average; the page shows coverage. Empty coverage displays an unavailable value rather than a measured zero.

A specific-meter filter shows that meter’s flow. A department filter selects its topmost linked meters, excluding selected descendants even on conditional routes. Department totals can overlap and are not additive site totals. Different circuits serving the same fixtures do not establish how much each fixture consumed. Tank inlet/outlet counters measure movement, not stored volume or tank level. Unmetered rainwater, storage changes and unmetered outlets prevent a complete site water balance; no leakage, storage level or exact end-use claim is derived.

Electricity has no topology in this app: selected electricity meters are summed. If electricity submeters overlap, configure a separate electricity boundary before interpreting that sum as site electricity.

## Reports and environmental indicators

Totals include complete covered days; averages divide by those covered days. Comparisons require complete coverage for both comparison periods and a positive prior value. Calendar-month charts compare matched month-to-date daily averages. Missing days remain chart gaps rather than invented values. Date filters are bounded to supported dates, at most approximately ten years and no later than today; adjusted dates are shown.

Excel separates the summary, raw captures, waste events, estimated daily flow and supporting interval evidence. Raw capture differences are full intervals and must not be added as filtered-period usage. Daily rows mark whether they contribute to the summary. Waste remains recorded events and is not interpolated.

Per-person indicators require full period coverage and a positive recorded people count. The water indicator is supplied water per recorded person, not proven personal consumption. Electricity carbon estimates also require a positive configured `Environmental:GridEmissionFactorKgCo2ePerKwh`; no placeholder factor is used.

## Units and historical limitations

The existing app contract uses litres for water and kWh for electricity. Verify each physical meter scale before entering readings. This change does not introduce per-meter conversion factors: counters marked m³/kL must be consistently converted to the app’s litres before capture. Integer counters cannot retain fractional input units. Never mix scales within one meter history.

Current active meter metadata and the current network boundary apply to historical reports. Network revisions are not effective-dated reporting boundaries. Disabled meter history is visible individually but excluded from active aggregate reporting. Reconcile historical exports before changing source assignments, departments or active status.

## Deployment and validation

Apply existing asset/network migrations as needed, then run `UtilityManagementApp/Scripts/UpgradeIntervalConsumption.sql` before deploying this application change. It adds the derived interval view and replaces reading procedures, preserving captures. The first nondeleted capture becomes the baseline; counter neighbours are checked on insert/update and the stored previous-counter chain is repaired after insert/update/soft-delete. SQL Server must support `CREATE OR ALTER` and window functions.

Run:

- `dotnet run --project tests/Consumption.Validation/Consumption.Validation.csproj`
- `dotnet run --project tests/WaterNetwork.Validation/WaterNetwork.Validation.csproj`
- `node --test tests/network-editor.test.mjs tests/water-network.test.mjs`

The consumption suite uses actual service/page-model code with fake data contracts and reconciles the exported workbook to displayed totals. It does not execute SQL. An isolated Razor host validates compilation separately. The web project builds independently of SSDT after the navigation/admin cleanup; the SQL project remains in the solution for database deployment. Live SQL migration and database concurrency must be verified against SQL Server before production. No live database was changed by this work.
