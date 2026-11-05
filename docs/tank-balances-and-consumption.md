# Tank balances, transfers and final consumption

This document supersedes the earlier assumption that Main Meter measures municipal inflow. The confirmed arrangement has **two separate physical meters**: a municipal inlet whose readings are not captured, and the recorded Main outlet leaving source storage. Main's counter change includes water sent to Lower Station and transfers to Top Station. It is not incoming municipal volume or final consumption.

## The three views

1. **Tank balances** show opening stock, water received, water leaving and closing stock. Outflow may be a transfer. Unknown stock or an unrecorded boundary stays unknown.
2. **Recorded flow / energy** keeps previous/current meter counters and their differences. Water rows are labelled as source outlet, tank/transfer, borehole supply, confirmed consumption, or unclassified flow. The database `Usage` field remains compatible; its value is not automatically described as consumed water.
3. **Water consumption · final-use meters only** appears on Dashboard, Reports, Meter Readings and Water Balances. It includes only network points explicitly marked as final consumption, with active linked water meters and non-overlapping boundaries. Source outlets, borehole inlet/outlet counters, storage inlets and downstream transfer counters are excluded. Legacy registry defaults of `NetworkRole=EndUse` do not silently classify every meter as consumption.

A valid consumption boundary cannot feed another metered point or tank. A storage counter must be identified as its outlet before it can represent final use. A combined municipal/borehole final-use counter may show total consumption, while the split between its source volumes remains unknown. Missing intervals remain unknown; measured zero is retained. The consumption table total requires full-period coverage across its selected counters, while individual rows show their own coverage. It covers confirmed metered uses, not an assurance that every unmetered site use has been captured.

## Corrected example: Header sends 2,000 L across all five outlets

These are illustrative period volumes after counter differences and unit normalization, not imported site measurements. Opening measurements are explicit assumptions for this example.

| Tank | Opening stock L | Water received L | Water leaving L | Closing stock L |
|---|---:|---:|---:|---:|
| Main/source tank | 0 | Unknown: inlet not recorded | 30,000 | Unknown |
| Docking Bay | 0 | 20,000 | 15,000 | 5,000 |
| Reservoir | 20,000 | 15,000 | 20,000 | 15,000 |
| Dosing Tank | 0 | 20,000 | 20,000 | 0 |
| Header Tank | 0 | 20,000 | 2,000 total | 18,000 |

Top Station's balance is 20,000 L opening stock + 20,000 L received = 38,000 L closing stock + 2,000 L recorded final consumption, provided those outlet readings measure final use rather than filling more tanks. The automated test splits the 2,000 L across five example counters; it does not assume actual outlets each consume an equal amount.

Main releases 30,000 L, of which 20,000 L is transferred to Top. The remaining 10,000 L is a Lower Station balance difference; it is not automatically labelled consumed without accounting for storage and other flows. If municipal inlet captures later record 100,000 L received with measured zero opening source stock and 30,000 L leaving, then 70,000 L source closing stock becomes calculable. The software does not invent the 100,000 L while the inlet is unrecorded.

## Configure measurements

In the Admin network editor, open a physical tank/source point's **Storage balance and final-use measurements** section. Enter a physically measured stock in litres and its end-of-day date. Select the actual inlet and outlet counters. Main's own counter belongs on the **outlet** side of the source tank; it is disabled as that tank's inlet. Its separate inlet remains unlinked/unrecorded until actual readings are collected. Do not mark all flows confirmed while an inlet, outlet, rain contribution or other path is unmeasured.

A receiving tank's inlet counter can also debit its supplying tank where it measures the same physical transfer. Each counter can credit one receiving tank and debit one supplying tank. Do not subtract both an overall source outlet and its downstream branches as separate withdrawals from the source tank. For source storage, use municipal inlet flow in and Main outlet flow out. Its downstream distribution is then analysed separately.

The opening stock column is calculated at the **start of the selected reporting period** from the measured anchor and intervening covered flows. It is not an older anchor copied beside this week's flows. When all required data is present, every displayed row reconciles: period opening + period inflow − period outflow = period closing. A direct physical measurement at the end date may establish closing stock even when an earlier period opening remains unknown.

To populate the consumption table, mark **This counter measures final consumption, not a tank transfer** at the appropriate linked end-use points. Supply and transfer counters cannot be made consumption just by selecting their readings in a report. All counters must already use the app's declared litre convention. The old workbook contains different multipliers and carried-forward cells; verify those physical unit factors and original capture status before any future import. No workbook values are imported by this change.

## Existing maps and deployment

The existing network JSON is upgraded on load with `SourceBoundaryVersion=1`. A legacy Main root acquires one unlinked municipal inlet upstream; Main's existing point ID, actual meter link and reading history are retained. The upgrade is idempotent and does not create a database meter or readings. Main is labelled as a source outlet. Layout shifts preserve the relative positions within that station. Saving the reviewed map persists the upgraded document through the existing revision check.

The added inlet role is optional for future capture and does not block Main outlet or final-consumption reporting when no inlet reading exists. Existing source-stock settings that used Main as an inlet display a review message instead of a false closing stock, and the editor prevents saving Main as its own tank inlet. No new SQL schema or stored-procedure deployment is required. No live database data was changed here.

Validation covers missing municipal input, 30,000 L outlet flow, the corrected Header total, Top stock conservation, independent consumption-only selection, period-clipped opening balances, legacy-map upgrades and the editor's outlet/inlet guard. Browser validation uses synthetic SQLite/Identity data and checks the four consumption-table locations, unknown source input with known outlet flow, the five tank columns, source/transfer exclusion, network source trace, editor serialization and mobile width. External chart CDNs are blocked in those checks; their pre-existing missing-Chart errors are allowed only as that known dependency limitation. The native station SVG curves render independently.
