-- Check the role.
SELECT
    Id,
    Name,
    NormalizedName,
    ConcurrencyStamp
FROM dbo.AspNetRoles
ORDER BY Name;

-- Check the user.
SELECT
    Id,
    UserName,
    NormalizedUserName,
    Email,
    NormalizedEmail,
    SecurityStamp
FROM dbo.AspNetUsers
WHERE Email = 'your-admin-email@example.com';

-- Check the user's assigned roles.
SELECT
    u.Email,
    r.Name AS RoleName,
    r.NormalizedName
FROM dbo.AspNetUsers AS u
INNER JOIN dbo.AspNetUserRoles AS ur
    ON ur.UserId = u.Id
INNER JOIN dbo.AspNetRoles AS r
    ON r.Id = ur.RoleId
WHERE u.Email = 'your-admin-email@example.com';
