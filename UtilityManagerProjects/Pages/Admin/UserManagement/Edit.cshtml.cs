using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Services;
namespace UtilityManagerProjects.Pages.Admin.UserManagement;
[Authorize(Roles="Admin")]
public sealed class EditModel(UserAdministration administration,UserManager<IdentityUser> users,SignInManager<IdentityUser> signIn) : PageModel
{
    [BindProperty(SupportsGet=true)] public string? UserId { get; set; }
    [BindProperty,Required,EmailAddress,StringLength(255)] public string Email { get; set; } = "";
    [BindProperty,DataType(DataType.Password),StringLength(100,MinimumLength=6)] public string? Password { get; set; }
    public async Task<IActionResult> OnGetAsync() {
        if(UserId is not null) { var user=await users.FindByIdAsync(UserId);if(user is null) return NotFound();Email=user.Email??""; }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync() {
        if(UserId is null && string.IsNullOrEmpty(Password)) ModelState.AddModelError(nameof(Password),"Enter an initial password.");
        if(UserId is not null && await users.FindByIdAsync(UserId) is null) return NotFound();
        if(!ModelState.IsValid) return Page();
        IdentityResult result;
        try { result=UserId is null?await administration.Create(Email,Password!):await administration.Update(UserId,Email); }
        catch(DbException) { ModelState.AddModelError("","Unable to save this user. Check the database connection and try again.");return Page(); }
        if(!result.Succeeded) { foreach(var error in result.Errors) ModelState.AddModelError("",error.Description);return Page(); }
        if(UserId==users.GetUserId(User)) { var user=await users.FindByIdAsync(UserId!);if(user is not null) await signIn.RefreshSignInAsync(user); }
        TempData["StatusMessage"]="User saved.";return RedirectToPage("./Index");
    }
}
