> Updated physical boundary: Main Meter is the source outlet. The separate municipal inlet is not recorded. For current roles, tank balances and consumption-only tables, see [tank-balances-and-consumption.md](tank-balances-and-consumption.md). Earlier examples below illustrate arithmetic and must not be interpreted as measured municipal receipts.

# Follow the water through Lower and Top Station

The Water Network diagram opens with **Both stations** selected. It keeps their coordinate spaces apart and draws the route between stations. Each linked point shows selected-period flow alongside the latest cumulative counter by the selected end date. Select a point for its possible source routes, upstream/downstream connections and reading history. Dates use the same interval allocation as Reports and Dashboard.

**Water balances** now contains separate Lower Station and Top Station sections, a receiving-meter view of transfers between stations, individual daily-average curves, and expandable source traces. No station-wide total is made by summing every counter. A transfer measured at Docking Bay is already inside upstream municipal supply; it is not added again as extra consumption. Receiving-counter flow alone does not prove all water dispatched at the other end arrived, nor measure transfer loss.

## Compare the next branch, not every descendant

The comparison table stops at the next physical metered point on each branch, including points whose actual meter is not yet linked. It compares parent and branch values over their **common covered dates**, showing the comparison day count. Individual meter totals can cover a longer interval; they are displayed separately. Missing intervals are unknown, and real zero movement remains zero.

An unlinked parent is not bypassed in favor of summing grandchildren. A downstream counter with another possible inlet cannot be wholly debited to either parent. Unmetered terminal branches also prevent an invented complete comparison. Source restrictions preserve municipal-only basin paths when a shared location also receives borehole flushing water.

A comparison difference is a measurement difference. It can include changing storage, unmetered paths, losses, or capture problems. It is **not automatically consumption, stock remaining, or a leak**. Drawing from stock can legitimately make downstream flow exceed new source inflow during the same period.

## The 2,000 L tank example

If the original tank physically starts with 2,000 L, it sends 500 L to the son and 600 L to the daughter, leaving **900 L** in the original tank. If the daughter passes 350 L into storage, she retains 250 L. Storage passes 200 L to the assistant, retaining 150 L. With five example houses receiving 10 L each, the assistant retains 150 L. If the son passes 100 L to each of five friends, he retains zero.

| Point | Measured opening L | Received L | Passed on / used L | Remaining L |
|---|---:|---:|---:|---:|
| Original tank | 2,000 | 0 | 1,100 | 900 |
| Son | 0 | 500 | 500 | 0 |
| Daughter | 0 | 600 | 350 | 250 |
| Storage | 0 | 350 | 200 | 150 |
| Assistant | 0 | 200 | 50 | 150 |

Remaining water is 1,450 L, and measured final delivery to the five friends plus five houses is 550 L: together 2,000 L. The five houses and complete empty opening measurements at later stages are illustrative test assumptions, not site measurements.

This case is tested with the Main counter unchanged during the period: the handovers use existing stock. The storage calculator uses the explicit 2,000 L physical opening stock and records 900 L remaining. The parent/branch flow comparison separately shows a -1,100 L difference, correctly indicating drawdown rather than inventing negative consumption. A cumulative counter reading of 2,000 by itself would not establish a 2,000 L opening stock.

A second test uses 2,000 L **newly passing through Main** during the period. Main compares against only Son 500 + Daughter 600 = 1,100 L. Later handovers remain beneath those branches; they are not added again against Main. That case has a +900 L parent/branch difference whose physical explanation still depends on storage measurements.

## Borehole Incoming and Out work as a pair

Set their reporting boundaries to **Borehole Incoming** and **Borehole Out** and link the actual meters. With a simple inlet-to-outlet route, the app pairs them automatically; no opening stock is needed to compare their measured movement.

- Incoming flow = incoming closing counter minus incoming opening counter.
- Water supplied from the borehole tank = Out closing counter minus Out opening counter.
- Matched measured difference = incoming flow minus Out flow over matching covered dates.

The raw absolute In and Out counters are never subtracted from each other. Their baselines can be unrelated. If 700 L entered and 350 L left, the paired difference is +350 L. If 100 L entered and 350 L left, it is -250 L, consistent with drawing down previously stored water. Actual remaining stock still needs an opening measurement and complete flow boundaries. Out measures water supplied from the tank, which may subsequently be stored or transferred rather than immediately consumed.

An incomplete pair does not hide the known side. If Out has comparable captures but Incoming does not, Out flow remains visible and the difference stays unknown. Extra inlets/outlets or a disconnected pair require checking the network configuration before treating them as a simple pair.

Municipal branches trace to Main Meter. Borehole water traces to its own inlet; Fire Hydrant and rain inputs retain their separate origins. A combined-source meter shows its possible source paths without assuming percentages or forcing independent borehole/rain water into Main's municipal counter. The trace shows one mapped route per possible root; it does not infer historical valve operation or measured source proportions.

## Setup and validation

Existing network links, reporting roles and stored readings are used. No new SQL schema or stored-procedure update is introduced by this change. The previous Waste Collections procedure upgrade remains a separate deployment requirement for that earlier feature. Current network configuration applies to selected historical dates; historical network configurations are not versioned.

Validation commands:

- `dotnet build UtilityManagerProjects/UtilityManagerProjects.csproj`
- `dotnet run --project tests/Consumption.Validation`
- `dotnet run --project tests/WaterNetwork.Validation`
- `node --test tests/station-water-flow.test.mjs tests/network-editor.test.mjs tests/water-network.test.mjs tests/source-accounting.test.mjs tests/reading-chart.test.cjs`
- Run the isolated synthetic SQLite/Identity host with `dotnet run --project tests/Admin.Validation -- --serve`, then the station-flow, network-editor and app-UI browser tests. These use `NODE_PATH` for Playwright and `CHROMIUM_EXECUTABLE_PATH` where required.

The synthetic UI fixture includes paired borehole movement (700 L in, 350 L out) and a measured 200 L receipt at Top Station. Browser validation checks both station sections/curves, the matching borehole difference, both-station map selection, cross-station links, historical end-date counters and mobile page width. The new SVG curves require no chart CDN. External CDNs are blocked during the browser checks.

The linked SharePoint workbook could not be read through the available connection. Its formulas and historical figures have not been inspected or imported. This implementation is based on the existing repository and the confirmed descriptions of site flow.
