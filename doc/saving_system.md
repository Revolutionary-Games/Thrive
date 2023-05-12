Saving System
=============

This document describes the main points of how saves work in Thrive.

JSON Serialization
------------------

The saving and loading is based on JSON serializing and deserializing
the data.

### Used Serializer for Type

Serializing uses the runtime types of objects to pick the serializer
to use to write them out as JSON. This ensures that the derived class
type having any attributes for how it is deserialized work entirely
correctly. This should be easy to understand but the tricky part is
that deserialization uses the **static** type of the object to be
deserialized.

So either the field type (fields can be assigned an object of derived
type, which is why this matters), or the type passed to the
deserialize call as the root object type is used instead of the
dynamic runtime type, which means that the static base class type used
for deserialization must be configured with a compatible deserializer
when compared to the actual derived type. Using plain `object` won't
even use the Thrive customized serializer so that's suitable for only
really simple data deserialization.

For use of `UseThriveSerializerAttribute` ask "does this have objects
that refer up to this object?" (i.e. circular references or descendant
objects referring back to their ancestors). If not then
`UseThriveSerializerAttribute` is unnecessary. Also some dynamic type
situations need that attribute, but you should only add
`UseThriveSerializerAttribute` *after* finding the need for it. See
the dedicated section on references for details about objects
referring to each other.

When a type has `JSONAlwaysDynamicTypeAttribute` the type should most
of the time also have `UseThriveSerializerAttribute` as otherwise the
always dynamic type doesn't work.

### Object References

When using `[JsonObject(IsReference = true)]` that means "write an ID
for this object in JSON", the ID can be referred to later. So this
means that *only* use that attribute for object types that can be
referred to multiple times in the JSON, otherwise we are just wasting
space writing object IDs in the resulting JSON that won't be
used. Also having a ton of reference IDs will actually slow down
deserialization a lot, so overusing this feature when not required
will slow down loading a lot.

When objects refer to each other in a loop (see the properties section
on how to break these loops to make them load) or with references back
up to their ancestor objects, `UseThriveSerializerAttribute` is
required to use the Thrive serializer which supports these use cases.

### Properties

Public fields and properties with a get operation are saved by
default. Private fields and properties with private setters need
`JsonPropertyAttribute` on them to load them from a save. Note that a
property with a public get will be written to the save **but will not
be loaded** unless it has a public set or that JSON attribute.

Properties and fields are serialized and deserialized in the order
they are defined in the C# source code file. The only exception is
that when the Thrive serializer is used, then constructor parameters
are deserialized always before running the constructor. This has the
slight complication that to break object loops the constructor
parameters should be placed first in a class. This way an object loop
can be loaded correctly by just deserializing the fields needed to
call the constructor first, and only after that the properties that
require the object loop to be rebuilt are defined.

For the above case the default JSON serializer would always fail but
the Thrive serializer will work when constructor parameters and
property order is picked carefully (or a separate constructor is added
that needs less properties and marked with
`JsonConstructorAttribute`).

If you add a new constructor parameter all properties that are defined
before that parameter now get loaded before the constructor is
executed. This is because the json loader needs to keep reading and
loading the properties until it has found all the constructor
parameters. For this reason all the constructor properties should be
put first in a class in the order they are used by the
constructor. This reduces the need to allocate temporary memory and
reduces the chance that accidental object loops that cannot be handled
are created.

Save File Format
----------------

Thrive saves are actually just `.tar.gz` files with the extension
changed. The files contain three separate files: JSON of the save
general info, the full save JSON, and a screenshot. The screenshot and
info exist separately so that the load game menu can easily show
previews.
