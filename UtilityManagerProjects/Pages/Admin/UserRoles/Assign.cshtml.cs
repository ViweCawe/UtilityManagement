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

        public AssignModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
        }

        [BindProperty(SupportsGet = true)]
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        [BindProperty]
        public List<RoleSelection> Roles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user =
                await userManager.FindByIdAsync(UserId);

            if (user == null)
            {
                return NotFound();
            }

            await LoadRolesAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user =
                await userManager.FindByIdAsync(UserId);

            if (user == null)
            {
                return NotFound();
            }

            var currentRoles =
                await userManager.GetRolesAsync(user);

            var selectedRoles = Roles
                .Where(x => x.IsSelected)
                .Select(x => x.RoleName)
                .ToList();

            var rolesToAdd = selectedRoles
                .Except(
                    currentRoles,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rolesToRemove = currentRoles
                .Except(
                    selectedRoles,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rolesToAdd.Any())
            {
                var addResult =
                    await userManager.AddToRolesAsync(
                        user,
                        rolesToAdd);

                if (!addResult.Succeeded)
                {
                    AddErrors(addResult);
                    await LoadRolesAsync(user);
                    return Page();
                }
            }

            if (rolesToRemove.Any())
            {
                var removeResult =
                    await userManager.RemoveFromRolesAsync(
                        user,
                        rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    AddErrors(removeResult);
                    await LoadRolesAsync(user);
                    return Page();
                }
            }

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
