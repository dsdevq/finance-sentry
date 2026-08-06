namespace FinanceSentry.Core.Domain;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public virtual Guid Id { get; protected init; }

    /// <summary>
    /// Date and time the entity was created (UTC).
    /// </summary>
    public virtual DateTime CreatedAt { get; protected init; }

    /// <summary>
    /// Date and time the entity was last modified (UTC).
    /// </summary>
    public virtual DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected Entity(Guid id) : this()
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
