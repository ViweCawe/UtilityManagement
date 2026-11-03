using Microsoft.AspNetCore.Authorization;
namespace UtilityManagerProjects.Pages.Area;
[Authorize(Roles="Admin")]
public sealed class CreateModel(UtilityManagerProjects.Services.ReferenceDataStore store) : Admin.ReferenceEditorModel(store)
{ protected override bool IsArea => true; }
