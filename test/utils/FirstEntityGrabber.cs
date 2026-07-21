namespace Test.Utils;

using System;
using Arch.Core;

/// <summary>
///   Grabs the first entity from a query
/// </summary>
public class FirstEntityGrabber
{
    private Entity found = Entity.Null;

    public FirstEntityGrabber(QueryDescription query, World world, bool allowMultiple = false)
    {
        world.Query(query,
            entity =>
            {
                if (found != Entity.Null)
                {
                    if (!allowMultiple)
                        throw new InvalidOperationException("Multiple entities match the given query");

                    return;
                }

                found = entity;
            });

        if (found == Entity.Null)
            throw new InvalidOperationException("No entities match the given query");
    }

    public Entity Found => found;
    public Entity Result => found;
}
