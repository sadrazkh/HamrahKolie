namespace HamrahKolie.Web.Areas.Admin.ViewModels;

public record RoleListItem(
    string Id,
    string Name,
    string? DisplayName,
    string? Description,
    bool IsSystemRole,
    int PermissionCount);

public record UserListItem(
    string Id,
    string? Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string Roles);
