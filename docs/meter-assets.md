# Creating meters and tanks

Before deploying this change, run `UtilityManagementApp/Scripts/UpgradeMeterAssets.sql` against the intended application database in SSMS. It adds AssetKind, NetworkRole and CapacityLitres and updates the insert/read/update procedures. It does not delete records. Deploy the app only after the script succeeds; old procedures cannot persist the new fields. The migration has not been executed against your live database by this change.

The database project also contains the schema/procedure definitions for fresh deployments. Existing rows default to Meter / End use; inspect and assign their actual roles. Water=1 and Electricity=2 match the C# enum; the misleading schema comment is corrected without reassigning any existing data.

| Asset kind | Allowed roles | Meaning |
|---|---|---|
| Meter | Source, Transfer, Distribution, End use | A device measuring flow/usage, including a flow meter attached to a tank |
| Tank | Storage, Distribution | Physical water storage, optionally also distributing to other points |

Tank capacity is optional and expressed in litres. It is not current stored volume. Tanks must be Water, cannot be cumulative meters, and are rejected by the usage-reading creation handler, including forged requests. Track a flow meter on a tank as a separate Meter record. A future tank-level capture workflow must use separate measurements; this change does not pretend that cumulative meter readings measure tank levels.

Suggested starting classifications from the board:

- Docking Bay Tank and Reservoir / Holding Tank: Tank / Storage.
- Header Tank: Tank / Distribution.
- A municipal incoming flow meter: Meter / Source.
- A meter measuring cable-car water transfer: Meter / Transfer.
- Branch meters serving downstream points: Meter / Distribution.
- Outlet meters: Meter / End use.

Only create an outlet as a Meter when a measuring device exists there. Confirm Garage Roof and other ambiguous labels on site. Classification records the role; it does not yet assign physical feed connections or automatically prevent double counting in aggregate reports. Follow the network measurement-boundary guidance before changing totals.

Create and Edit expose the classification and optional capacity, validate combinations on the server, and persist through Dapper. The register shows the saved kind and role. The new update procedure prevents converting an asset with existing meter readings between Meter and Tank.

The forms retain the app's navy `#152436` / `#0d1d31` and blue `#0d6efd` palette. Navy panels use white/light text; input fields use white backgrounds with dark text. Selection and keyboard focus have visible borders, and errors use light red on navy (dark red on pale error panels).

Validation: classification rules are checked in the standalone C# validation project. The full application still has the existing external SQL-project reference build limitation documented in `water-network.md`. Database migration execution and full browser visual verification remain required in your environment.
