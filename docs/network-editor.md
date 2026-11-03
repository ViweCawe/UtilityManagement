# Edit the network by following the water

The editor has separate tasks for adding a point, rearranging an existing point and reviewing the flow. Admin access is required. No code or Figma links are needed to edit it.

## Connect a new meter or outlet

1. Create the actual water meter or metered tank in **Meters → Add Meter or Tank** first if it does not exist.
2. Open **Water Network → Edit network** and choose **Connect a new meter or outlet**.
3. Select an unused meter (or Unmetered outlet), enter its name/role, choose the upstream supplying point, and optionally enter the usage area and purpose.
4. Choose **Connect this new point**. Review it in the flow section. The parent determines its initial station; its details allow a station change.
5. Choose **Save reviewed network** after checking the full map.

Only external source/storage inlets should have no upstream supplier. Adding a point does not create a meter record, and adding an unmetered outlet does not create a reading. Each actual meter can be linked once. A point can later gain or lose its meter assignment using **Record a meter at this point**; meter records and history remain intact.

## Rearrange an existing point

Choose **Change suppliers for an existing point**, select that point and edit its current supplier routes. **Add another supplier** records a genuine second source. Each route identifies its supplier, normal/conditional operation, use (basins/flushing/etc.), source restriction and operating note. Removing a route changes the map only. The meter identity and reading history stay with the point.

A normal connection may also describe a limited backup use: Main Meter normally supplies Level 1 basins, with flushing only when the borehole runs out. It must not be hidden entirely as a backup route because basins use it normally. Workshop basins/fountains use a municipal-only circuit restriction; borehole supply serves flushing. The trace respects these restrictions.

## Review sources, branches and shared areas

The flow review groups municipal, borehole, separate hydrant, shared downstream and other points. Suppliers precede their descendants inside the sections. Shared-source parents and their downstream circuits stay together; each point shows its supplying names and source description. Open a point to edit name, station, role, linked meter, usage area or notes.

**Reporting boundary** controls the source rows: Main Meter municipal total, Borehole Out supplied water, Borehole Incoming storage inflow, and separate Fire Hydrant flow. Each role is unique. For replacement of a reporting point, clear its old role before assigning a new one. An inlet role cannot have a normal upstream supplier. Reporting roles and circuit restrictions are checked in both JavaScript and C#.

The default municipal KPI is Main Meter only. It is not a sum of Main Meter, Garage Roof, tanks and outlets, or a sum with borehole storage inflow. Source rows are independent and must not be added indiscriminately. See consumption-calculations.md for the metric definitions.

Workshop and Level 1 have combined-source counters. Their source split is shown as not separately measured. If real separate inlet counters are installed, connect those meters to the corresponding source and give them the same **Usage area**. The reporting code then uses exclusive inlet counters over matching covered dates. Source restrictions describe the actual pipe circuit; they are not a way to assign a percentage of a mixed counter.

## Save and persistence

The map and its complete meter-assignment snapshot still save atomically to WaterNetworkDocument/WaterNetworkLinks. Revision conflicts reject stale editors; reload the saved map before retrying. Cycles, invalid/missing route endpoints, duplicate assignments/routes, missing conditional notes, duplicate reporting roles and incompatible source restrictions are rejected. Current Lower/Top station layouts remain supported.

Circuit metadata uses the existing JSON document. Legacy documents are clarified once on load for the confirmed Main/borehole basin/flushing arrangement and acquire CircuitDefinitionsVersion 1; subsequent saves retain user edits. No SQL migration is needed specifically for these new fields. Existing asset/network/editor/interval/admin migrations are still required for their respective features.

Changing suppliers, reporting roles, meter links or usage areas affects how existing readings are grouped: this is the current map, not an effective-dated historical map. Keep reconciled exports when the physical layout changes. Removing a point removes its routes/assignment but preserves meter records/readings. Cancel/reload discards unsaved work. Save arranges diagram coordinates automatically.

## Verification

- Consumption validation checks the Main control total, independent storage inlet/outlet/hydrant quantities, unmeasured combined-source splits, dedicated inlet measurements and aligned coverage/exports.
- Node tests check source-aware tracing, impossible restrictions, reporting roles and source-ordered editor groups.
- C# network validation checks topology, assignments and legacy circuit upgrade.
- `tests/network-editor-browser.test.cjs` uses the synthetic Admin.Validation host to connect an unused actual meter, change its supplier, add a second source, validate/serialize the reviewed map and check the mobile layout. Its POST is intercepted; it does not persist anything in a live SQL database.

Production SQL save/concurrency and the physical meter boundaries still require verification in the target environment. This work did not modify the live database.
