using HamrahKolie.Application.Authorization;

namespace HamrahKolie.Tests.Authorization;

/// <summary>
/// آزمون‌های یکپارچگی کاتالوگ نقش‌ها و دسترسی‌ها.
/// این آزمون‌ها از خطاهای رایج مثل کلید تکراری یا ارجاع به دسترسی ناموجود جلوگیری می‌کنند.
/// </summary>
public class RbacCatalogTests
{
    [Fact]
    public void Permission_keys_are_unique()
    {
        var duplicates = Permissions.All
            .GroupBy(p => p.Key)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Every_role_permission_references_a_defined_permission()
    {
        var validKeys = Permissions.All.Select(p => p.Key).ToHashSet();

        foreach (var role in Roles.All)
        {
            // نقش Super Admin از نشانگر «*» استفاده می‌کند و مستثناست.
            if (role.Permissions.Length == 1 && role.Permissions[0] == "*") continue;

            foreach (var key in role.Permissions)
            {
                Assert.True(validKeys.Contains(key),
                    $"نقش «{role.Name}» به دسترسی ناموجود «{key}» ارجاع داده است.");
            }
        }
    }

    [Fact]
    public void Role_names_are_unique()
    {
        var duplicates = Roles.All
            .GroupBy(r => r.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void SuperAdmin_role_is_defined_and_uses_wildcard()
    {
        var superAdmin = Roles.All.SingleOrDefault(r => r.Name == Roles.SuperAdmin);
        Assert.NotNull(superAdmin);
        Assert.Equal(new[] { "*" }, superAdmin!.Permissions);
    }
}
