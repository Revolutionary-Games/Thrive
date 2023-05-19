using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Allows safely keeping references to game entities across multiple frames. Needs to be used instead of raw
///   references as those can't clear themselves when the entity is disposed
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class EntityReference<T>
    where T : class, IEntity
{
    // TODO: should this be somehow set to null when we detect that the alive marker is no longer alive
    // Currently set to clear on fetch
    private T? currentInstance;
    private AliveMarker? currentAliveMarker;

    public EntityReference(T value)
    {
        Value = value;
    }

    /// <summary>
    ///   Creates a reference that doesn't refer to anything
    /// </summary>
    public EntityReference()
    {
    }

    /// <summary>
    ///   Gets the referenced entity if it is still alive
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If you need to access one object a bunch of times it's better to get the value from here once per frame
    ///     and keep a normal reference around.
    ///   </para>
    /// </remarks>
    public T? Value
    {
        get
        {
            if (currentAliveMarker == null)
                return null;

            if (currentAliveMarker.Alive)
                return currentInstance;

            // Clear these references to let objects get garbage collected
            currentInstance = null;
            currentAliveMarker = null;
            return null;
        }
        set
        {
            if (currentInstance == value)
                return;

            if (value == null)
            {
                currentInstance = null;
                currentAliveMarker = null;
            }
            else
            {
                var marker = value.AliveMarker;

                if (marker.Alive)
                {
                    currentInstance = value;
                    currentAliveMarker = marker;
                }
                else
                {
                    currentInstance = null;
                    currentAliveMarker = null;
                }
            }
        }
    }

    [JsonIgnore]
    public bool IsAlive => currentAliveMarker is { Alive: true };

    public static implicit operator T?(EntityReference<T> value)
    {
        return value.Value;
    }

    public static bool operator ==(EntityReference<T> lhs, EntityReference<T>? rhs)
    {
        return Equals(lhs, rhs);
    }

    public static bool operator !=(EntityReference<T> lhs, EntityReference<T>? rhs)
    {
        return !(lhs == rhs);
    }

    public static bool operator ==(EntityReference<T> lhs, T? rhs)
    {
        return lhs.Value == rhs;
    }

    public static bool operator !=(EntityReference<T> lhs, T rhs)
    {
        return !(lhs == rhs);
    }

    public void ClearIfNotAlive()
    {
        if (IsAlive)
            return;

        Value = null;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (GetType() != obj.GetType())
        {
            return false;
        }

        return Equals((EntityReference<T>)obj);
    }

    public bool Equals(EntityReference<T> obj)
    {
        return ReferenceEquals(Value, obj.Value);
    }

    public override int GetHashCode()
    {
        if (currentInstance == null)
            return 19;

        return EqualityComparer<T>.Default.GetHashCode(currentInstance);
    }
}
