# Meter page integration

Copy the four page files into `Pages/Meters`, replacing the current empty Edit page and current Index page.

## Required data contract

The supplied pages use the APIs already visible in the uploaded Create page and make these two additional assumptions:

```csharp
// DataLibrary.Models.Meter
public bool IsActive { get; set; }

// DataLibrary.Data.IMeterData
Task UpdateMeter(Meter meter);
```

`UpdateMeter` must update `MeterName`, `MeterType`, `AreaId`, `DepartmentId`, `StationId`, `IsCumulative`, and `IsActive` for the matching `Id`. Keep the command parameterized. If the method returns `Task<int>` instead of `Task`, the page code still works because the result is intentionally ignored.

New meters should be active by default. Prefer a database default on `IsActive`; otherwise add `IsActive = true` to the `Meter` initializer in `Create.cshtml.cs`.

## Last-updated value

The index recognizes these meter projection properties, in order:

- `LastReadingAt`
- `UpdatedAt`
- `ModifiedAt`
- `DateUpdated`
- `UpdatedDate`

For the most useful result, add `DateTimeOffset? LastReadingAt` to `Meter` and populate it in `GetMeters` from the latest reading timestamp. The query should use a grouped aggregate or `OUTER APPLY` so it does not create one query per meter. If no recognized timestamp is present, the UI deliberately displays `Not recorded`.

## Pagination scope

Pagination is implemented on the supplied Meters Index page with stable `Id DESC` ordering, search, page-size selection, and query-state preservation. Because the current contract only exposes `GetMeters()`, filtering and paging happen after that call. For a large register, move filtering, `COUNT(*)`, ordering, `OFFSET`, and `FETCH` into a paged `IMeterData` query.

Only the Meters Index was supplied, so other application Index pages are not changed by this package. Apply the same GET parameters and stable-ordering pattern to those page-specific queries rather than sharing a reflection-heavy generic repository.

## Verification checklist

- Open `/Meters` with zero, one, and more than 10 meters.
- Search by meter name, numeric ID, and type; clear the search.
- Change page size and navigate backward/forward.
- Edit each field and confirm invalid IDs are rejected server-side.
- Disable a meter from both Edit and Index; confirm it remains visible and reading history remains intact.
- Confirm disabled meters are rejected when a new reading is submitted.
- Confirm timestamps render in the application server's configured local timezone.
