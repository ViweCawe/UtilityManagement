# Edit the water network without code

## Use the editor

1. Create a new water meter or metered tank using **Meters → Add Meter or Tank** if it does not exist yet.
2. Open **Water Network → Edit network** as an Admin.
3. Choose **Add point**, enter its name and station, choose its role and select its actual meter. Leave it unassigned while setting it up, or turn off **Has a meter** for an unmetered outlet.
4. Choose **Add connection**. Select the supplying point under **From** and the receiving point under **To**. For a conditional supply, enter when it operates.
5. Choose **Save network**. The app arranges the diagram and returns to the viewing page, including the assigned meters' saved readings.

Existing names, roles, descriptions, meter assignments and connections are editable. Remove a connection with its own button. Removing a point also removes its incoming and outgoing diagram connections. Meter records and reading history are preserved. Cancel discards unsaved edits; **Reload saved map** opens the latest saved revision. Adding a point does not itself create a physical meter record.

There must be at least one point. Each meter can be assigned once. Cycles, duplicate connections, missing endpoints, and conditional connections without conditions are rejected. Lower and Top remain the supported stations. Diagram positions are arranged automatically rather than entered as coordinates. Editing requires JavaScript; viewing retains the connection register for use without JavaScript.

## Deployment and storage

Apply `UpgradeMeterAssets.sql`, then `UpgradeWaterNetworkLinks.sql` if needed, then **`UtilityManagementApp/Scripts/UpgradeWaterNetworkEditor.sql`** before deploying the new page/service. The SQL project includes the table/procedure definitions for fresh deployments as well. The editor migration is rerunnable and preserves saved maps, meter records and readings. It has not been executed against the live database here.

`WaterNetworkDocument` stores one JSON map with a revision, last update time and editing user. Before the first map save, the application uses the shipped JSON map as a seed. Saved maps take precedence after deployment. `WaterNetworkLinks` remains the assignment source for viewing and historical meter links. The editor loads existing assignments, including legacy configuration; a save writes a complete assignment snapshot and map in one SQL transaction. Removed points lose their assignments, without deleting the meter or its readings.

Both full-map saves and independent meter-link edits lock the same document row and increment its revision. An editor opened before a later save/link change is rejected on save, allowing the user to reload the latest version rather than overwrite it. A unique filtered index protects one-meter-per-point assignment, and SQL checks active water meters again inside the save. Razor authorization restricts editing to Admin; form POSTs use antiforgery protection. The server revalidates client input and layout bounds; the UI never exposes raw JSON editing.

## Verification

15 Node tests cover route tracing, source changes, user-created points without Figma identifiers, loops, duplicate assignments/routes, conditional routes, automatic station layout and removal/cascade behaviour. C# validation also rejects null points, invalid assignments and invalid topology. An isolated ASP.NET Core 10 host compiles the changed Razor pages, store and page models against the repository's actual data contracts. A DOM interaction check covers adding a metered point, creating a route, form serialization/reload, automatic layout, rejecting invalid routes and removing a point with its connections.

Full solution build, real SQL migration/transaction/concurrency execution, authenticated end-to-end database interaction and browser visual layout remain unverified. The isolated compile/DOM checks do not establish those results. Run the migration and exercise save/reload, restart persistence, and two-editor conflict handling in the target test database before production deployment.
