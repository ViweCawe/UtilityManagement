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

The default water KPI and graph use **Main Meter municipal flow only**. Filtering by Main Meter gives the same control total. Downstream distribution/tank/outlet counters are not additional water and must not be added to it. The default is no longer the previous sum of Main Meter, Fire Hydrant and borehole storage inflow. This definition applies consistently to Dashboard, Reports, Environmental, Usage, Analytics, Readings summaries and the export summary.

| Reporting role | What its reading measures |
| --- | --- |
| Main Meter | Controlling municipal supply flow |
| Borehole Out | Water leaving borehole storage for supply/use |
| Borehole Incoming | Water entering storage; not its remaining level |
| Fire Hydrant | Separate municipal branch outside Main Meter |

These independent boundaries appear separately with their own date coverage. Do not add borehole inlet and outlet as additional consumption. Storage changes, rainwater and unmetered branches mean child measurements need not sum exactly to Main Meter, even over matching dates. No leakage or stored-volume claim is inferred.

The editor's **Reporting boundary** setting identifies these roles. One metered point may hold each role. Legacy point identities supply defaults; users can move a role to another verified point by clearing the old role and setting the new one. A combined-source counter cannot represent the exclusively municipal or borehole boundary. Missing borehole/hydrant assignments do not suppress Main Meter flow. Missing Main Meter assignment or interval data does.

A meter filter shows its own recorded flow, including combined-source flow when applicable. A department filter uses the topmost exclusive municipal meters on the Main Meter branch. It excludes borehole and hydrant flow and rejects an unknown municipal contribution from a combined counter unless an included municipal parent already covers it. Department figures are not additive site totals.

### Shared usage areas and circuits

Workshop and Level 1 Visitor Toilets have combined meters. Municipal water supplies basins; borehole water normally supplies flushing. Municipal backup flushing operates when the borehole runs out. The map records normal municipal basin supply and describes the backup flushing condition. Circuit restrictions prevent the borehole trace from flowing into municipal-only Workshop basins/fountains.

The **Usage area** field groups points for reporting. With one combined meter the recorded total remains visible, while municipal and borehole portions are **not separately measured**. Parent readings and arbitrary percentages cannot identify simultaneous source contributions. Exact separation requires real dedicated inlet counters or equivalent source-specific measurements. The app creates no synthetic readings or assumed proportions.

If separate inlet meters are installed, add/connect the actual meters to their suppliers, assign the same usage area and set each route's water-source restriction correctly. Source amounts are calculated only from exclusive, mapped counters, with parent/child overlap removed. Area columns use the same commonly covered dates; independent inlet columns therefore reconcile over that common coverage. Flow measured by a combined parent does not become a separately measured source value.

Circuit purpose and source restrictions, reporting roles and usage areas are stored in the existing revisioned network JSON. Legacy documents receive the known circuit clarification once through `CircuitDefinitionsVersion=1`, including normal Main Meter supply for Level 1 basins and borehole flushing. The viewing page receives the clarification too. A subsequent save persists it, and versioned user edits are not repeatedly overwritten. This does not delete points, meter links or reading records, and requires no new SQL schema migration.

Electricity has no topology in this app: selected electricity meters are summed. If electricity submeters overlap, configure a separate electricity boundary before interpreting that sum as site electricity.

## Reports and environmental indicators

Totals include complete covered days; averages divide by those covered days. Comparisons require complete coverage for both comparison periods and a positive prior value. Calendar-month charts compare matched month-to-date daily averages. Missing days remain chart gaps rather than invented values. Date filters are bounded to supported dates, at most approximately ten years and no later than today; adjusted dates are shown.

Excel separates the summary, raw captures, waste events, estimated daily flow and supporting interval evidence. Raw capture differences are full intervals and must not be added as filtered-period usage. Daily rows mark whether they contribute to the summary. Waste remains recorded events and is not interpolated.

Per-person indicators require full period coverage and a positive recorded people count. The default water indicator is Main Meter municipal flow per person; borehole outlet flow is displayed separately. The water indicator is supplied water per recorded person, not proven personal consumption. Electricity carbon estimates also require a positive configured `Environmental:GridEmissionFactorKgCo2ePerKwh`; no placeholder factor is used.

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
