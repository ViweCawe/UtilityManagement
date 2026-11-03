using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UtilityManagerProjects.Data;
namespace UtilityManagerProjects.Services;

// Identity and employee audit references use the same configured database and transaction.
public sealed class UserAdministration(ApplicationDbContext db,UserManager<IdentityUser> users,RoleManager<IdentityRole> roles)
{
    static IdentityResult Fail(string message) => IdentityResult.Failed(new IdentityError {Description=message});
    public async Task<IdentityResult> Create(string email,string password)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user=new IdentityUser {UserName=email.Trim(),Email=email.Trim()};
        var result=await users.CreateAsync(user,password);
        if(!result.Succeeded) return result;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dbo.Employees(Email,UserId,IsActive) VALUES({user.Email},{user.Id},1)");
        await transaction.CommitAsync();return result;
    }
    public async Task<IdentityResult> Update(string id,string email)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user=await users.FindByIdAsync(id);
        if(user is null) return Fail("User not found.");
        email=email.Trim();
        if(!string.Equals(user.Email,email,StringComparison.OrdinalIgnoreCase)) user.EmailConfirmed=false;
        user.Email=email;user.UserName=email;
        var result=await users.UpdateAsync(user);
        if(!result.Succeeded) return result;
        result=await users.UpdateSecurityStampAsync(user);
        if(!result.Succeeded) return result;
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Employees SET Email={email} WHERE UserId={id}");
        await transaction.CommitAsync();return result;
    }
    public async Task<IdentityResult> Delete(string id,ClaimsPrincipal actor)
    {
        if(id==users.GetUserId(actor)) return Fail("You cannot delete your own signed-in account.");
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user=await users.FindByIdAsync(id);
        if(user is null) return Fail("User not found.");
        if(await users.IsInRoleAsync(user,"Admin") && (await users.GetUsersInRoleAsync("Admin")).Count<=1) return Fail("Keep at least one Admin account.");
        // Preserve employee IDs attached to past readings instead of deleting their audit history.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Employees SET IsActive=0 WHERE UserId={id}");
        var result=await users.DeleteAsync(user);
        if(!result.Succeeded) return result;
        await transaction.CommitAsync();return result;
    }
    public async Task<IdentityResult> SetRoles(string id,IEnumerable<string> selected,ClaimsPrincipal actor)
    {
        var requested=selected.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user=await users.FindByIdAsync(id);
        if(user is null) return Fail("User not found.");
        foreach(var role in requested) if(!await roles.RoleExistsAsync(role)) return Fail("A selected role no longer exists. Reload the page.");
        var current=await users.GetRolesAsync(user);
        if(current.Contains("Admin") && !requested.Contains("Admin",StringComparer.OrdinalIgnoreCase)) {
            if(id==users.GetUserId(actor)) return Fail("Another Admin must change your own Admin role.");
            if((await users.GetUsersInRoleAsync("Admin")).Count<=1) return Fail("Keep at least one Admin account.");
        }
        var result=await users.AddToRolesAsync(user,requested.Except(current,StringComparer.OrdinalIgnoreCase));
        if(!result.Succeeded) return result;
        result=await users.RemoveFromRolesAsync(user,current.Except(requested,StringComparer.OrdinalIgnoreCase));
        if(!result.Succeeded) return result;
        result=await users.UpdateSecurityStampAsync(user);
        if(!result.Succeeded) return result;
        await transaction.CommitAsync();return result;
    }
}
