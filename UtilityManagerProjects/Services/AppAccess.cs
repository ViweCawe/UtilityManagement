using Microsoft.AspNetCore.Identity;
namespace UtilityManagerProjects.Services;
public static class AppAccess
{
    public static IMvcBuilder AddUtilityPages(this IServiceCollection services)
    {
        services.Configure<SecurityStampValidatorOptions>(options=>options.ValidationInterval=TimeSpan.Zero);
        services.AddAuthorization(options=>options.AddPolicy("AdminOnly",policy=>policy.RequireRole("Admin")));
        return services.AddRazorPages(options=> {
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToPage("/Index");
            options.Conventions.AllowAnonymousToPage("/Error");
            foreach(var page in new[]{"/Meters/Create","/Meters/Edit"})options.Conventions.AuthorizePage(page,"AdminOnly");
            foreach(var folder in new[]{"/Admin","/Area","/Stations","/MeterAreas","/Imports"}) options.Conventions.AuthorizeFolder(folder,"AdminOnly");
        });
    }
}
