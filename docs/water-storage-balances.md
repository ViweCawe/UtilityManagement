> Updated physical boundary: Main Meter is the source outlet. The separate municipal inlet is not recorded. For current roles, tank balances and consumption-only tables, see [tank-balances-and-consumption.md](tank-balances-and-consumption.md). Earlier examples below illustrate arithmetic and must not be interpreted as measured municipal receipts.

# Water movement, stock and final use

A cumulative counter reading is not tank stock. Main Meter going from 100,000 to 200,000 L records 100,000 L supplied over that interval. The first reading is a baseline. Daily flow between captures is the interval difference divided across the elapsed days; dates after the last capture remain unknown.

The new **Water balances** page separates recorded source flow, internal transfers, estimated tank stock and recorded final consumption. Main Meter remains the municipal control total; downstream counters are never added to it. Fire Hydrant and borehole reporting retain their separate boundaries.

## Configure physical measurements

In **Network → Edit → Existing points**, expand **Storage balance and final-use measurements** for each tank/source. Enable physical stored water only where a physical storage volume exists. Enter an actual measured opening stock in litres and its end-of-day date. Never copy the counter value into this field.

Select all inlet and outlet counters. With one meter per tank, the receiving tank's inlet counter can also measure its supplying tank's outlet **only if that meter covers that same physical transfer**. Assign it once on each side. A shared transfer must follow a route on the map. A counter cannot credit multiple receiving tanks or debit multiple supplying tanks.

Confirm complete flows only after accounting for every inlet, outlet, rain contribution, use and loss. Do not select it if an unmeasured path exists. A mixed-source downstream counter cannot be wholly debited to a tank supplied by just one of those sources.

Mark **final consumption** only on a counter that measures water actually used, rather than water stored in another tank or passed through another meter. One inlet counter alone cannot establish how much subsequently left a terminal tank. For example, a toilet tank inlet of 2,000 L does not establish 500 L consumed. That requires a measured outlet/use boundary or an independently measured stock change. This implementation calculates stock from configured counter boundaries; it does not infer consumption from tank levels.

## Accounting example (illustrative measurements)

Assume all tanks were physically measured empty at the opening date, all flows are covered by matching counter intervals, and every flow is measured:

| Point | Inflow L | Outflow L | Closing stock L |
|---|---:|---:|---:|
| Main storage | 100,000 | 20,000 | 80,000 |
| Docking Bay | 20,000 | 15,000 | 5,000 |
| Reservoir | 15,000 | 10,000 | 5,000 |
| Dosing Tank | 10,000 | 10,000 | 0 |
| Header Tank | 10,000 | 4,400 | 5,600 |
| Toilets Tank | 2,000 | 500 | 1,500 |
| Three further branches, each | 800 | 200 | 600 |

Header outflow is 2,000 + three × 800. Recorded final use is 500 + three × 200 = 1,100 L. Total remaining stock is 98,900 L; stock plus final use reconciles to 100,000 L supplied. Transfers cancel across tank boundaries instead of becoming extra consumption. These are test fixtures, not imported site readings.

Unknown opening stock, incomplete interval coverage, unconfirmed boundaries or negative calculated stock produce an explanation instead of invented litres remaining. Physical opening stock on the report end date can be shown directly. The selected period's inflow/outflow is distinct from closing stock, which uses all covered flow since the opening measurement. Counter-derived daily stock is an interval estimate, not a measured physical tank level. All inputs must already be litres; there is no automatic unit conversion or tank-capacity/overflow inference.

Configuration is saved in the existing network document. It is current configuration applied to the selected historical period; historical topology and opening-stock revisions are not versioned. Record a suitable opening stock for the period being investigated. No water database schema changes are required.

# Waste collection repair and deployment

Waste amounts are separate positive collection weights in kg; sum events, never difference or interpolate them as water counters. Create preserves the selected collection date. Edit can correct the date. Delete is an audited soft deletion and preserves database history. Inactive waste types remain visible in historical records, but cannot be selected for new collection entries. Invalid submissions retain the active type dropdown. Search applies to both current and comparison periods. Zero previous amounts show unavailable growth, not a fabricated percentage. Category splits are exclusive; ambiguous recycling/landfill categories go to Other, and category/material charts retain the remainder beyond their top five.

Before deploying the app, apply `UtilityManagementApp/Scripts/UpgradeWasteCollections.sql` to the intended SQL Server database (or deploy the five updated stored procedures via the SQL project). This rerunnable script alters procedures only; it does not delete or rewrite historical data. It adds the optional edited collection date and audited deletion parameters and expands insert notes to the existing table's 500-character size. **The updated app expects these procedure signatures.**

The SQL Server procedures have not been executed against a live database in this workspace. Back up and validate them in your normal SQL deployment environment. Existing incorrectly dated events cannot be reconstructed automatically; correct their collection dates using the edit page where the original date is known.

Validation: `dotnet run --project tests/Consumption.Validation`, `dotnet run --project tests/Waste.Validation`, `dotnet run --project tests/WaterNetwork.Validation`, and the Node network/chart tests. Browser checks use the isolated Admin.Validation SQLite/Identity test host with synthetic data, not the site's database.
