using Microsoft.AspNetCore.Identity;

namespace UtilityManagerProjects.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider services,
            string adminEmail)
        {
            using var scope = services.CreateScope();

            var roleManager =
                scope.ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames =
            {
                "Admin",
                "Manager",
                "User"
            };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(roleName));
                }
            }

            var admin =
                await userManager.FindByEmailAsync(adminEmail);

            if (admin != null &&
                !await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(
                    admin,
                    "Admin");

                // Updating the security stamp invalidates old cookies.
                await userManager.UpdateSecurityStampAsync(admin);
            }
        }
    }
}
