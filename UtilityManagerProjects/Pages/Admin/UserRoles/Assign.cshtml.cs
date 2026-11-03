using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Pages.Admin.UserRoles
{
    [Authorize(Roles = "Admin")]
    public class AssignModel : PageModel
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UtilityManagerProjects.Services.UserAdministration administration;

        public AssignModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager, UtilityManagerProjects.Services.UserAdministration administration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
            this.administration=administration;
        }

        [BindProperty(SupportsGet = true)]
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        [BindProperty]
        public List<RoleSelection> Roles { get; set; } = new();

        public IActionResult OnGet() => RedirectToPage("/Admin/UserManagement/EditRoles",new {userId=UserId});

        public async Task<IActionResult> OnPostAsync()
        {
            var user =
                await userManager.FindByIdAsync(UserId);

            if (user == null)
            {
                return NotFound();
            }

            if(!ModelState.IsValid) { await LoadRolesAsync(user);return Page(); }
            IdentityResult result;
            try { result=await administration.SetRoles(UserId,Roles.Where(r=>r.IsSelected).Select(r=>r.RoleName),User); }
            catch(System.Data.Common.DbException) { ModelState.AddModelError("","Unable to update roles. Reload and try again.");await LoadRolesAsync(user);return Page(); }
            if(!result.Succeeded) { AddErrors(result);await LoadRolesAsync(user);return Page(); }

            var currentUserId =
                userManager.GetUserId(User);

            if (string.Equals(
                currentUserId,
                user.Id,
                StringComparison.Ordinal))
            {
                await signInManager.RefreshSignInAsync(user);
            }

            TempData["StatusMessage"] =
                $"Roles for '{user.Email}' were updated.";

            return RedirectToPage("./Index");
        }

        private async Task LoadRolesAsync(
            IdentityUser user)
        {
            UserEmail =
                user.Email
                ?? user.UserName
                ?? "Unknown user";

            var currentRoles =
                await userManager.GetRolesAsync(user);

            var allRoles = await roleManager.Roles
                .OrderBy(x => x.Name)
                .ToListAsync();

            Roles = allRoles
                .Select(role => new RoleSelection
                {
                    RoleName = role.Name!,
                    IsSelected = currentRoles.Contains(
                        role.Name!,
                        StringComparer.OrdinalIgnoreCase)
                })
                .ToList();
        }

        private void AddErrors(
            IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }
        }

        public class RoleSelection
        {
            public string RoleName { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
        }
    }
}
