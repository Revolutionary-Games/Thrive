using System.Collections.Generic;

public interface IReadOnlyMetaballLayout<T> : IReadOnlyCollection<T>
    where T : IReadOnlyMetaball
{
}
