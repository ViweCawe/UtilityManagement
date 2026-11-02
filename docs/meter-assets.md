# Creating meters and tanks

Before deploying this change, run `UtilityManagementApp/Scripts/UpgradeMeterAssets.sql` against the intended application database in SSMS. It adds AssetKind, NetworkRole and CapacityLitres and updates the insert/read/update procedures. It does not delete records. Deploy the app only after the script succeeds; old procedures cannot persist the new fields. The migration has not been executed against your live database by this change.

The database project also contains the schema/procedure definitions for fresh deployments. Existing rows default to Meter / End use; inspect and assign their actual roles. Water=1 and Electricity=2 match the C# enum; the misleading schema comment is corrected without reassigning any existing data.

| Asset kind | Allowed roles | Meaning |
|---|---|---|
| Meter | Source, Transfer, Distribution, End use | A device measuring flow/usage, including a flow meter attached to a tank |
| Tank | Source, Storage, Distribution | Metered physical water storage, optionally also distributing to other points |

Tank capacity is optional and expressed in litres. It is not current stored volume. Tanks must be Water. Their attached meter readings can be cumulative and are accepted by the usage-reading form. The tank record can represent the tank and its attached meter for reading capture. A future tank-level capture workflow must use separate measurements; this change does not pretend that cumulative meter readings measure tank levels.

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

The Main Meter is the municipal inflow source. There is no separate Municipality Supply meter record. The network page omits Figma links and uses “Water comes from”, “Water goes to”, and optional “View readings” actions. The stable internal key `main-tank` is retained for existing mapping configuration; it now means Main Meter — Source and does not assert that the point is a tank.

Metered tank clarification: tanks have attached meters and their readings are captured through the meter-reading form. Tank roles include Source, Storage and Distribution; cumulative readings are allowed. Capacity remains a separate optional specification. Re-run UpgradeMeterAssets.sql to replace the earlier restrictive check constraint. This supersedes earlier statements excluding tank readings.
