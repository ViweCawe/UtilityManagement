using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Pages.Admin.UserManagement
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [BindProperty]
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string NewRoleName { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        public List<IdentityRole> Roles { get; set; } = new();
        public List<UserRow> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        public async Task<IActionResult> OnPostCreateRoleAsync()
        {
            NewRoleName = NewRoleName.Trim();

            if (!ModelState.IsValid)
            {
                await LoadPageAsync();
                return Page();
            }

            if (await roleManager.RoleExistsAsync(NewRoleName))
            {
                ModelState.AddModelError(
                    nameof(NewRoleName),
                    $"The role '{NewRoleName}' already exists.");

                await LoadPageAsync();
                return Page();
            }

            // Use RoleManager rather than inserting directly into AspNetRoles.
            // IdentityRole initializes NormalizedName and ConcurrencyStamp correctly.
            var result = await roleManager.CreateAsync(
                new IdentityRole(NewRoleName));

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await LoadPageAsync();
                return Page();
            }

            StatusMessage = $"Role '{NewRoleName}' was created.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteRoleAsync(string roleId)
        {
            var role = await roleManager.FindByIdAsync(roleId);

            if (role == null)
            {
                StatusMessage = "The selected role was not found.";
                return RedirectToPage();
            }

            var usersInRole =
                await userManager.GetUsersInRoleAsync(role.Name!);

            if (usersInRole.Count > 0)
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

        private async Task LoadPageAsync()
        {
            Roles = await roleManager.Roles
                .OrderBy(x => x.Name)
                .ToListAsync();

            var users = await userManager.Users
                .OrderBy(x => x.Email)
                .ToListAsync();

            Users = new List<UserRow>();

            foreach (var user in users)
            {
                var roles =
                    await userManager.GetRolesAsync(user);

                Users.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "Unknown user",
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.OrderBy(x => x).ToList()
                });
            }
        }

        public class UserRow
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool EmailConfirmed { get; set; }
            public List<string> Roles { get; set; } = new();
        }
    }
}
