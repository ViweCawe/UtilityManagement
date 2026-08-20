using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Pages.Admin.Roles
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<IdentityUser> userManager;

        public IndexModel(
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        [BindProperty]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string RoleName { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        public List<IdentityRole> Roles { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadRolesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            RoleName = RoleName.Trim();

            if (!ModelState.IsValid)
            {
                await LoadRolesAsync();
                return Page();
            }

            if (await roleManager.RoleExistsAsync(RoleName))
            {
                ModelState.AddModelError(
                    nameof(RoleName),
                    $"The role '{RoleName}' already exists.");

                await LoadRolesAsync();
                return Page();
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole(RoleName));

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await LoadRolesAsync();
                return Page();
            }

            StatusMessage = $"Role '{RoleName}' was created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            string roleId)
        {
            var role = await roleManager.FindByIdAsync(roleId);

            if (role == null)
            {
                StatusMessage = "The selected role was not found.";
                return RedirectToPage();
            }

            var usersInRole =
                await userManager.GetUsersInRoleAsync(role.Name!);

            if (usersInRole.Any())
            {
                StatusMessage =
                    $"The role '{role.Name}' cannot be deleted " +
                    "while users are assigned to it.";

                return RedirectToPage();
            }

            var result = await roleManager.DeleteAsync(role);

            StatusMessage = result.Succeeded
                ? $"Role '{role.Name}' was deleted."
                : string.Join(
                    " ",
                    result.Errors.Select(x => x.Description));

            return RedirectToPage();
        }

        private async Task LoadRolesAsync()
        {
            Roles = await roleManager.Roles
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}
