using RunningCompetition.Domain.Common;

namespace RunningCompetition.Domain.Entities;

/// <summary>Represents an application role.</summary>
public class Role : BaseEntity
{
    /// <summary>Gets or sets the unique role name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the normalized role name for lookups.</summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this is a system role (cannot be deleted).</summary>
    public bool IsSystem { get; set; }

    // Navigation
    /// <summary>Gets or sets the permissions assigned to this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    /// <summary>Gets or sets the users assigned to this role.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

/// <summary>Represents a granular permission.</summary>
public class Permission : BaseEntity
{
    /// <summary>Gets or sets the unique permission key (e.g., "users.view").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the feature/module group.</summary>
    public string Group { get; set; } = string.Empty;

    // Navigation
    /// <summary>Gets or sets the roles that have this permission.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

/// <summary>Join table linking roles to permissions.</summary>
public class RolePermission
{
    /// <summary>Gets or sets the role ID.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Gets or sets the permission ID.</summary>
    public Guid PermissionId { get; set; }

    // Navigation
    /// <summary>Gets or sets the role.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Gets or sets the permission.</summary>
    public Permission Permission { get; set; } = null!;
}

/// <summary>Join table linking users to roles.</summary>
public class UserRole
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the role ID.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Gets or sets when the role was assigned.</summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    /// <summary>Gets or sets the user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the role.</summary>
    public Role Role { get; set; } = null!;
}
