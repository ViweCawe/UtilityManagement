# Water network and saved meter readings

The initial network uses the updated Water Meter Mapper and the user's operational clarifications. It contains 33 points and 32 physical/annotated connections. Municipality is the origin, not another database meter. Main Meter is the metered source for the main supply. The stable mapping key `main-tank` is retained for compatibility.

## Link actual meters

Run `UtilityManagementApp/Scripts/UpgradeWaterNetworkLinks.sql` in SSMS after `UpgradeMeterAssets.sql`, before deploying this page. These scripts do not delete readings or meters. No live migration has been run here.

Sign in as Admin and open Water Network. Under **Link a point to its actual meter**, choose the diagram point and the existing water meter, then save. Metered tanks are eligible. Each meter can be assigned to one point; unmetered outlets and origin/transfer annotations cannot be assigned. To remove an assignment, choose that point and **Remove assignment**. Database assignments override legacy `WaterNetwork:MeterIds` configuration, including explicit removals.

The diagram and details show the latest saved counter value, unit, capture date and linked meter name/ID. The history action opens that meter's actual readings. A linked meter with no captured reading says **No readings yet**; an unassigned point says **Assign a meter**. Unmetered outlets say **No separate meter**. The displayed values refresh when the page reloads; this is not a live sensor feed. Loading uses bulk meter/link/latest-reading queries, not one query per node. Assignment writes require Admin and antiforgery protection. The database enforces unique meter assignments and a meter foreign key.

## Confirmed flow interpretation

- Fire Hydrant is a separately metered municipal branch, excluded from the Main Meter and main billed usage. Total municipal **period usage** is Main Meter usage plus Fire Hydrant usage over matching dates. Do not add their lifetime counter readings to calculate period usage. There is no inferred pipe from Main Meter to Fire Hydrant.
- Borehole Incoming represents the metered storage tank receiving drilled water. Borehole Out measures water leaving that storage. Its supply to toilets is for flushing; basins receive main water.
- Garage Roof is a parent distribution meter. Water passing it has already passed Main Meter; downstream meters show where that water goes.
- Level 1 Visitor Toilets normally use borehole water for flushing. Main Meter supplies flushing only when borehole water has run out. Level 1 is the destination, not a backup asset.
- Workshop Meter serves unmetered Basins, Fountains and Level 4 Toilets, and metered Old Cable Car. The new borehole-to-workshop route is labelled toilet flushing only. These annotations do not prove a separate meter on each internal circuit.
- Ticket Office Toilets is connected to Garage Roof in the updated board.
- Docking Bay, Reservoir and the other metered tanks capture flow through successive stages. Increasing counters do not indicate current tank levels. These internal transfer readings must not be summed as additional external supply.


## Validation and limits

Network integrity and route traversal tests pass. JavaScript syntax checks pass. Earlier isolated Razor compilation covered the previous revision; the local .NET SDK/temporary validation host is no longer available, so this revision has not been compiled here. SQL migration execution, actual Dapper round trips, linked meter readings against the live database, and browser visual layout remain unverified. The migration must be applied before opening the updated network page.
The resolved Connections to confirm section and the Map reviewed / municipal period usage footer were removed from the app page at the user’s request. Main Meter is the parent of Garage Roof, and Garage Roof is a parent distribution meter for its downstream areas.


## Edit the map in the app

Admins can choose **Edit network** on the viewing page. See [Network editor guide](network-editor.md). The JSON file now supplies the initial map only. After a user saves the editor, the SQL document is the authoritative map and subsequent deployments do not replace it. Meter assignments remain in WaterNetworkLinks, and saving the editor commits topology and assignments together.

Apply `UpgradeWaterNetworkEditor.sql` after the existing meter/link migrations and before deploying this version. This changes the network loading path to SQL persistence and updates the link-save procedure for concurrency. If you rerun an older link migration afterwards, rerun the editor migration last to restore its concurrency checks.
