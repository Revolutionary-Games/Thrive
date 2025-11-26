Saving System
=============

This document describes the main points of how saves work in Thrive.

Object Serialization
--------------------

The saving and loading is based on a custom binary format serializing
and deserializing the object data.

This is implemented in the RevolutionaryGamesCommon library.

### Serialization

To implement serialization a pair of methods is needed:
`WriteToArchive` and `ReadFromArchive`. This is a more explicit
implementation than a JSON-based approach, but with much less magic:
all properties are written and loaded exactly as instructed by each
class itself.

Class types should implement either the `IArchivable` or
`IArchiveUpdatable` interface (the second is for classes that are
created separately and only fill their properties from archives, for
example, Godot Nodes do this to avoid temporary Node allocations).

Primitive objects go through the `Write` method of the writer and the
specific variant of the reader. For example, `Write(2)` and then
`ReadInt32()`.

When writing references to objects use either `writer.WriteObject` or
`writer.WriteObjectOrNull` depending on if the object can be null or
not. Note that the or null interface does not support all collection
types! So some special types need first checking against null and an
explicit `writer.WriteNullObject()`. For example:

```c#
if (field != null)
{
    writer.WriteObject(specialCollectionField);
}
else
{
    writer.WriteNullObject();
}
```

You'll know this issue triggered if there's a cast error from `List`
to a more specific container type.

There's also a more advanced reader variant with extended type
information which can be used for templated classes. Look at
`HexLayoutSerializer` for an example.

### Object References

If an object is referred to multiple times in an archive it must set
this property to true:

```c#
public bool CanBeReferencedInArchive => true;
```

If a descendant can refer back up to its ancestor, then the
deserialization method needs to be written like this:

```c#
public static MyClass ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
{
    if (version is > SERIALIZATION_VERSION or <= 0)
        throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

    var instance = new MyClass();
    
    // Register the object reference so that things can point to it already
    reader.ReportObjectConstructorDone(instance);
        
    // And now read the properties
    instance.field2 = reader.ReadObjectOrNull<SomeObject>();

    return instance;
}
```

### Properties

With the archiving system nothing is explicitly saved, so all
properties that need to be saved or loaded must be done manually.

Here's a full example class:

```c#
public class MyClass : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string field1;

    private SomeObject? field2;

    public MyClass(string arg)
    {
        field1 = arg;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.MyClass;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MyClass)
            throw new NotSupportedException();

        writer.WriteObject((MyClass)obj);
    }

    public static MyClass ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new MyClass(reader.ReadString() ?? throw new NullArchiveObjectException())
        {
            field2 = reader.ReadObjectOrNull<SomeObject>(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(field1);
        writer.WriteObjectOrNull(field2);
    }
}
```

Note that not all classes require a write and read callbacks, but
you'll notice easily enough with errors about unknown type for
serialization / deserialization. When they are added they need to be
registered in the `ThriveArchiveManager` like this:

```c#
RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MyClass, typeof(MyClass),
    MyClass.WriteToArchive);
RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MyClass, typeof(MyClass),
    MyClass.ReadFromArchive);
```

If you don't register the callbacks, then also don't write them into
the file (as it is confusing if archiving methods exist that are not
actually used and can make troubleshooting a problem take extra time
to notice that something isn't registered).

### Components

Components have their own approach based on the `IArchivableComponent`
interface and implementation in their respective helper class. See
`WorldPosition` for an example and the `ComponentDeserializers` class.

### Versioning

This archiving system has a built-in way to do versioning. Each object
must report its current version. And on load it receives the version
information.

Here's an example how to correctly increase version from 1 to 2 and
write a new property:

```c#
public static MyClass ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
{
    if (version is > SERIALIZATION_VERSION or <= 0)
        throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

    var instance = new MyClass();

    reader.ReportObjectConstructorDone(reader.ReadString() ?? throw new NullArchiveObjectException());

    instance.field2 = reader.ReadObjectOrNull<SomeObject>();

    if (version > 1)
        instance.field3 = (CastedType)reader.ReadObjectOrNull(out var archiveType);

    return instance;
}

public void WriteToArchive(ISArchiveWriter writer)
{
    writer.Write(field1);
    writer.WriteObjectOrNull(field2);
    writer.WriteObjectOrNull(field3);
}
```

This example leaves `field3` to the default value but else could be
used to use some different value when loading older versions.

This system ensures that old saves are easy to keep compatible as long
as care is taken each time new properties are added.


Save File Format
----------------

Thrive saves are actually just `.tar.gz` files with the extension
changed. The files contain three separate files: JSON of the save
general info, the full save archive binary, and a screenshot. The
screenshot and info exist separately so that the load game menu can
easily show previews.
