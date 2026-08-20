using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace UtilityManagerProjects.Pages.Admin.UserRoles
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> userManager;

        public IndexModel(
            UserManager<IdentityUser> userManager)
        {
            this.userManager = userManager;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public List<UserRoleRow> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            var identityUsers = await userManager.Users
                .OrderBy(x => x.Email)
                .ToListAsync();

            foreach (var user in identityUsers)
            {
                var roles =
                    await userManager.GetRolesAsync(user);

                Users.Add(new UserRoleRow
                {
                    Id = user.Id,
                    Email =
                        user.Email
                        ?? user.UserName
                        ?? "Unknown user",
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles
                        .OrderBy(x => x)
                        .ToList()
                });
            }
        }

        public class UserRoleRow
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool EmailConfirmed { get; set; }
            public List<string> Roles { get; set; } = new();
        }
    }
}
