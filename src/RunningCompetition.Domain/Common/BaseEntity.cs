namespace RunningCompetition.Domain.Common;

/// <summary>
/// Base entity providing common audit and soft-delete fields for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the ID of the user who created this record.</summary>
    public Guid? CreatedById { get; set; }

    /// <summary>Gets or sets the UTC last-modified timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the ID of the user who last modified this record.</summary>
    public Guid? UpdatedById { get; set; }

    /// <summary>Gets or sets a value indicating whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this record was soft-deleted.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Gets or sets the ID of the user who deleted this record.</summary>
    public Guid? DeletedById { get; set; }

    /// <summary>Marks this entity as soft-deleted.</summary>
    public void SoftDelete(Guid deletedById)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedById = deletedById;
    }

    /// <summary>Updates the audit fields on modification.</summary>
    public void SetUpdated(Guid updatedById)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }
}
