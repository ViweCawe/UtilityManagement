# Water network — Figma review and app integration

Source: [Water Meter Mapper](https://www.figma.com/board/x7u5LA84S82rYAFgz67Qn2/Water-Meter-Mapper), reviewed 2 November 2026.

## App behaviour

Open **Water Network** in the sidebar or from **Meters** (`/WaterNetwork`, sign-in required). Select Lower or Top Station, then a point to see direct feeds, destinations and upstream/downstream routes. Follow a detail link to move between stations. The full connection register remains available without JavaScript.

The network contains 29 physical points and 27 connections. `App_Data/water-network.json` is the source for the diagram and register, with Figma node/connector IDs for traceability. Annotation-only shapes are not represented as pipework. The intermediate pumping and dosing descriptions are collapsed into physical tank-to-tank routes, retaining both connector IDs.

- Main Meter (Source; records municipal inflow) → Kitchen Stores, Garage Roof, Workshop / Staff Toilets / Fountains, and cable car transfer to Top Station.
- Garage Roof → Staff Room, Misting System, Ice Machine, Vista / Mezze, TAP Bar and Vida Café.
- Borehole Incoming → Borehole Out → Staff Toilets.
- Garage Roof → Staff Toilets is a **backup when the borehole supply is closed**. It remains visible as a dashed route but is excluded from the trace until selected. Selecting it explores a possible route; it does not assert valve status or simultaneous operation.
- Cable car transfer → Docking Bay Tank → Reservoir / Holding Tank → Dosing Tank / New Header Tank → Header Tank → Top Station outlets.
- Rainwater also enters the reservoir.
- Fire Hydrant is explicitly outside main-meter usage on the board. Its supply source is not drawn; no Main Tank connection is invented.

Vida Café's Garage Roof connector (`12:1305`) appeared during the review and was incorporated into this revision. It is no longer marked as an unresolved feed.

## Figma corrections applied

The existing layout and physical routes were preserved. Changes include:

- Corrected `Transfare` to `Transfer from Lower Station` and clarified the municipal source, receiver, pumping and distribution descriptions.
- Distinguished tank levels/storage from flow-meter readings.
- Made the Garage Roof backup route (`11:1204`) dashed and clarified its condition.
- Named the station sections and shortened corrected labels to fit their shapes.
- Removed two connectors with deleted targets: `9:451` → missing `9:458`, and `11:559` → missing `11:566`. No destination was guessed.

## Linking actual database meters

The map does not provide database meter IDs. All points start unlinked. A diagram point is not automatically a meter: it may be a tank, source, outlet or distribution point.

After verifying IDs on site, add explicit mappings under `WaterNetwork:MeterIds` in deployment configuration. Keys are stable JSON point IDs such as `main-tank`, `dosing` and `toilets-main`; values are positive IDs from the existing meter register. For example, set the environment variable `WaterNetwork__MeterIds__dosing` to the **verified** dosing meter ID. Do not use the Figma node number as a database ID.

The page uses the existing Dapper-backed `IMeterData.GetMeters()` contract to validate configured IDs as water meters and shows links to the existing history page. Invalid or duplicate mappings produce visible warnings. Disabled meters remain labelled and their history remains accessible. With no mappings, the page does not query the meter database. There is no schema change and no production data is written.

## Consumption and storage boundaries

This feature does not change existing dashboard/report arithmetic. Those pages currently sum `Usage` across selected meters, so aggregating a parent and its downstream meters may count the same water several times. The map alone does not establish which database readings belong to each boundary.

Before changing totals, confirm:

1. Database meter IDs, physical inlet/outlet locations, and actual reading units. Existing models use `L³` while reports use `L`; the map does not establish the correct conversion.
2. Whether Main Tank and Garage Roof labels denote tanks, meters, or both; the physical storage arrangement at the Main Meter source.
3. The hydrant supply and metering exclusion, grouped Workshop / Staff Toilets / Fountains branch, and fallback operating rules.
4. Tank capacities and level measurements, transfer volumes, rainwater measurements, and compatible reading intervals.

Keep imported supply, internal transfer, end-use consumption and stored volume as separate measures. A storage balance needs opening volume + measured inflows − measured outflows = closing volume (plus any measured/estimated losses). Flow readings cannot determine tank levels on their own, and source inflows into storage are not automatically end-use consumption in the same period.

## Validation

Run `node --test tests/water-network.test.mjs` for network integrity and route tracing. Run `dotnet run --project tests/WaterNetwork.Validation/WaterNetwork.Validation.csproj` for the C# loader and invalid-data guards. Tests cover municipal/rainwater separation, Top Station reachability, conditional backups, unresolved feeds, duplicate/dangling connections and cycles.

The new Razor page and page model were also compiled and exercised in an isolated ASP.NET Core 10 validation host using the repository's meter model/interface and shared layout. That host is temporary test scaffolding, not application code.

An HTTP smoke check confirmed the rendered register, embedded network data, and JavaScript/CSS assets (including the module MIME type). A DOM interaction check against that rendered page passed both station selectors, the backup toggle, Vida Café tracing, reservoir inputs, cross-station navigation and keyboard selection. Browser visual testing could not run because the environment blocks Chromium's required local socket; DOM checks do not verify layout. Figma corrections were visually checked using board screenshots.

The full application's existing build is blocked before compilation by `UtilityManagerProjects.csproj` referencing `..\..\UtilityManagementApp\UtilityManagementApp.sqlproj`, which resolves outside the repository. It also references the in-repository legacy SSDT project and a compiled DLL under `obj`. These references were not changed as part of the network feature. A successful isolated feature check is not a successful full application build or database integration test.

The Main Meter is the municipal inflow source. There is no separate Municipality Supply meter record. The network page omits Figma links and uses “Water comes from”, “Water goes to”, and optional “View readings” actions. The stable internal key `main-tank` is retained for existing mapping configuration; it now means Main Meter — Source and does not assert that the point is a tank.

Metered tank clarification: tanks have attached meters and their readings are captured through the meter-reading form. Tank roles include Source, Storage and Distribution; cumulative readings are allowed. Capacity remains a separate optional specification. Re-run UpgradeMeterAssets.sql to replace the earlier restrictive check constraint. This supersedes earlier statements excluding tank readings.
