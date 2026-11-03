using Microsoft.AspNetCore.Authorization;
namespace UtilityManagerProjects.Pages.Stations;
[Authorize(Roles="Admin")]
public sealed class EditModel(UtilityManagerProjects.Services.ReferenceDataStore store) : Admin.ReferenceEditorModel(store)
{ protected override bool IsArea => false; }
