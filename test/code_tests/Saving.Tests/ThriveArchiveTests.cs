namespace ThriveTest.Saving.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using global::Saving.Serializers;
using Godot;
using SharedBase.Archive;
using Xunit;

public class ThriveArchiveTests
{
    private readonly ThriveArchiveManager manager = new();

    // Very important to ensure type mapping is unique. If this fails, then the newly added bad types need numbers
    // incremented
    [Fact]
    public void ThriveArchive_TestObjectTypesAreUnique()
    {
        var seenTypes = new HashSet<int>();

        // Put base types in first
        foreach (var value in Enum.GetValues<ArchiveObjectType>())
        {
            Assert.True(seenTypes.Add((int)value));
        }

        // And then ensure all Thrive types are unique
        foreach (var value in Enum.GetValues<ThriveArchiveObjectType>())
        {
            if (value == ThriveArchiveObjectType.InvalidThrive)
                continue;

            if (!seenTypes.Add((int)value))
                Assert.Fail($"Duplicate archive object type for {value} = {(int)value}");
        }
    }

    [Fact]
    public void ThriveArchive_TestChunkSceneConfiguration()
    {
        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, manager);
        var reader = new SArchiveMemoryReader(memoryStream, manager);

        var original = new ChunkConfiguration.ChunkScene("res://random_scene.tscn");

        manager.OnStartNewWrite(writer);
        writer.WriteArchiveHeader(1, "test", "2");
        writer.WriteObject(original);
        writer.WriteArchiveFooter();
        manager.OnFinishWrite(writer);

        manager.OnStartNewRead(reader);
        memoryStream.Seek(0, SeekOrigin.Begin);
        reader.ReadArchiveHeader(out _, out _, out _);

        var read = reader.ReadObjectOrNull<ChunkConfiguration.ChunkScene>();
        reader.ReadArchiveFooter();
        manager.OnFinishRead(reader);

        Assert.Equal(original.ScenePath, read!.ScenePath);
    }

    [Fact]
    public void ThriveArchive_TestBiomeConditions()
    {
        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, manager);
        var reader = new SArchiveMemoryReader(memoryStream, manager);

        var original = new BiomeConditions(new Dictionary<Compound, BiomeCompoundProperties>
        {
            {
                Compound.Glucose, new BiomeCompoundProperties
                {
                    Ambient = 0,
                    Amount = 10,
                    Density = 100,
                }
            },
            {
                Compound.Sunlight, new BiomeCompoundProperties
                {
                    Ambient = 1,
                }
            },
            {
                Compound.Ammonia, new BiomeCompoundProperties
                {
                    Ambient = 0,
                    Amount = 10,
                    Density = 100,
                }
            },
        }, new Dictionary<Compound, BiomeCompoundProperties>
        {
            {
                Compound.Glucose, new BiomeCompoundProperties
                {
                    Ambient = 0,
                    Amount = 10,
                    Density = 100,
                }
            },
            {
                Compound.Sunlight, new BiomeCompoundProperties
                {
                    Ambient = 0.98f,
                }
            },
        }, new Dictionary<Compound, BiomeCompoundProperties>
        {
            {
                Compound.Glucose, new BiomeCompoundProperties
                {
                    Ambient = 0,
                    Amount = 10,
                    Density = 100,
                }
            },
            {
                Compound.Sunlight, new BiomeCompoundProperties
                {
                    Ambient = 0.76f,
                }
            },
        }, new Dictionary<Compound, BiomeCompoundProperties>
        {
            {
                Compound.Glucose, new BiomeCompoundProperties
                {
                    Ambient = 0,
                    Amount = 0,
                    Density = 100,
                }
            },
            {
                Compound.Sunlight, new BiomeCompoundProperties
                {
                    Ambient = 1,
                }
            },
        }, new Dictionary<Compound, BiomeCompoundProperties>
        {
            {
                Compound.Glucose, new BiomeCompoundProperties
                {
                    Ambient = 0,
                    Amount = 10,
                    Density = 100,
                }
            },
            {
                Compound.Sunlight, new BiomeCompoundProperties
                {
                    Ambient = 0,
                }
            },
        });

        original.Chunks = new Dictionary<string, ChunkConfiguration>
        {
            {
                "first", new ChunkConfiguration
                {
                    Name = "Chunks",
                    Density = 1234,
                    Meshes = [new ChunkConfiguration.ChunkScene("res://test_scene.tscn")],
                    Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>
                    {
                        {
                            Compound.Glucose, new ChunkConfiguration.ChunkCompound
                            {
                                Amount = 1000,
                            }
                        },
                    },
                    DissolverEnzyme = "stuff",
                    DamageType = string.Empty,
                }
            },
            {
                "second", new ChunkConfiguration
                {
                    Name = "Other stuff",
                    Density = 900,
                    Meshes = [new ChunkConfiguration.ChunkScene("res://test_scene2.tscn")],
                    DissolverEnzyme = "none",
                    DamageType = "toxin",
                }
            },
        };

        manager.OnStartNewWrite(writer);
        writer.WriteArchiveHeader(1, "test", "2");
        writer.WriteObject(original);
        writer.WriteArchiveFooter();
        manager.OnFinishWrite(writer);

        manager.OnStartNewRead(reader);
        memoryStream.Seek(0, SeekOrigin.Begin);
        reader.ReadArchiveHeader(out _, out _, out _);

        var read = reader.ReadObjectOrNull<BiomeConditions>();
        reader.ReadArchiveFooter();
        manager.OnFinishRead(reader);

        Assert.NotNull(read);

        // We don't care about speed in tests
        // ReSharper disable UsageOfDefaultStructEquality
        Assert.True(original.Compounds.SequenceEqual(read.Compounds));
        Assert.True(original.MinimumCompounds.SequenceEqual(read.MinimumCompounds));
        Assert.True(original.MaximumCompounds.SequenceEqual(read.MaximumCompounds));
        Assert.True(original.AverageCompounds.SequenceEqual(read.AverageCompounds));
        Assert.True(original.CurrentCompoundAmounts.SequenceEqual(read.CurrentCompoundAmounts));

        Assert.Contains("first", read.Chunks);
        Assert.Contains("second", read.Chunks);
        Assert.NotNull(original.Chunks["first"].Compounds);
        Assert.NotNull(read.Chunks["first"].Compounds);
        Assert.True(original.Chunks["first"].Compounds!.SequenceEqual(read.Chunks["first"].Compounds!));

        // Chunk scenes are classes so equals check would fail
        Assert.Equal(original.Chunks["first"].Meshes.Count, read.Chunks["first"].Meshes.Count);
        Assert.Equal(original.Chunks["second"].Meshes.Count, read.Chunks["second"].Meshes.Count);

        // ReSharper restore UsageOfDefaultStructEquality
    }

    [Fact]
    public void ThriveArchive_TestWorldSerialization()
    {
        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, manager);
        var reader = new SArchiveMemoryReader(memoryStream, manager);

        var originalWorld = World.Create();
        var originalEntity1 = originalWorld.Create();

        // It's apparently safe to refer to numeric types from Godot in the tests
        originalEntity1.Add(new WorldPosition(Vector3.Left, Quaternion.Identity));
        originalEntity1.Add(new PlayerMarker());

        manager.OnStartNewWrite(writer);
        writer.WriteAnyRegisteredValueAsObject(originalWorld);
        manager.OnFinishWrite(writer);

        manager.OnStartNewRead(reader);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var read = reader.ReadObjectOrNull<World>();
        manager.OnFinishRead(reader);

        Assert.NotNull(read);
        Assert.Equal(originalWorld.Size, read.Size);

        Entity readEntity = Entity.Null;

        read.Query(new QueryDescription().WithAll<PlayerMarker>(), entity =>
        {
            if (readEntity != Entity.Null)
                Assert.Fail("Found duplicate entities");

            readEntity = entity;
        });

        Assert.NotEqual(Entity.Null, readEntity);
        Assert.Equal(originalEntity1.Get<WorldPosition>(), readEntity.Get<WorldPosition>());
        Assert.Equal(originalEntity1.Get<PlayerMarker>(), readEntity.Get<PlayerMarker>());
    }

    [Fact]
    public void ThriveArchive_DictionaryWithVector2IKeys()
    {
        var memoryStream = new MemoryStream();
        var writer = new SArchiveMemoryWriter(memoryStream, manager);
        var reader = new SArchiveMemoryReader(memoryStream, manager);

        var original = new Dictionary<Vector2I, List<string>>
        {
            {
                new Vector2I(1, 2),
                [
                    "test",
                    "test2",
                ]
            },
            {
                new Vector2I(-1, 50),
                [
                    "another thing",
                ]
            },
            {
                new Vector2I(0, 0),
                new List<string>()
            },
        };

        manager.OnStartNewWrite(writer);
        writer.WriteObject(original);
        manager.OnFinishWrite(writer);

        manager.OnStartNewRead(reader);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var read = reader.ReadObjectOrNull<Dictionary<Vector2I, List<string>>>();
        manager.OnFinishRead(reader);

        Assert.NotNull(read);
        Assert.Equal(original.Count, read.Count);

        Assert.True(original[new Vector2I(1, 2)].SequenceEqual(read[new Vector2I(1, 2)]));
        Assert.True(original[new Vector2I(-1, 50)].SequenceEqual(read[new Vector2I(-1, 50)]));
        Assert.True(original[new Vector2I(0, 0)].SequenceEqual(read[new Vector2I(0, 0)]));
    }

    [Fact]
    public void ThriveArchive_TestReadableNameBaseInterface()
    {
        var memoryStream = new MemoryStream();

        var specialManager = new ThriveArchiveManager();
        specialManager.RegisterObjectType(ArchiveObjectType.TestObjectType1,
            typeof(TestReadableNameBaseInterface), TestReadableNameBaseInterface.WriteToArchive);
        specialManager.RegisterObjectType(ArchiveObjectType.TestObjectType1,
            typeof(TestReadableNameBaseInterface), TestReadableNameBaseInterface.ReadFromArchive);

        var writer = new SArchiveMemoryWriter(memoryStream, specialManager);
        var reader = new SArchiveMemoryReader(memoryStream, specialManager);

        var originalList1 = new List<IPlayerReadableName>
        {
            new TestReadableNameBaseInterface(1),
        };

        var originalList2 = new List<IPlayerReadableName>();

        var originalList3 = new List<IPlayerReadableName>
        {
            new TestReadableNameBaseInterface(1),
            new TestReadableNameBaseInterface(2),
        };

        var originalList4 = new List<IPlayerReadableName>
        {
            new TestReadableNameBaseInterface(1),
            new TestReadableNameBaseInterface(2),
            new TestReadableNameBaseInterface(-1),
        };

        void TestList(List<IPlayerReadableName> list)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);

            specialManager.OnStartNewWrite(writer);
            writer.WriteObject(list);
            specialManager.OnFinishWrite(writer);

            specialManager.OnStartNewRead(reader);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var readList = reader.ReadObjectOrNull<List<IPlayerReadableName>>();
            specialManager.OnFinishRead(reader);

            Assert.NotNull(readList);
            Assert.Equal(list.Count, readList.Count);
            Assert.True(readList.SequenceEqual(list));

            if (list.Count > 0)
            {
                Assert.Equal(list[0].ReadableName, readList[0].ReadableName);
            }
        }

        TestList(originalList1);
        TestList(originalList2);
        TestList(originalList3);
        TestList(originalList4);
    }

    private class TestReadableNameBaseInterface(int index)
        : IPlayerReadableName, IArchivable, IEquatable<TestReadableNameBaseInterface>
    {
        public const ushort SERIALIZATION_VERSION = 1;

        public string ReadableName => $"I'm a readable thing {Index}";

        public int Index { get; set; } = index;

        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

        public ArchiveObjectType ArchiveObjectType => ArchiveObjectType.TestObjectType1;

        public bool CanBeReferencedInArchive => true;

        public static bool operator ==(TestReadableNameBaseInterface? left, TestReadableNameBaseInterface? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TestReadableNameBaseInterface? left, TestReadableNameBaseInterface? right)
        {
            return !Equals(left, right);
        }

        public static TestReadableNameBaseInterface ReadFromArchive(ISArchiveReader reader, ushort version,
            int referenceId)
        {
            if (version is > SERIALIZATION_VERSION or <= 0)
                throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

            return new TestReadableNameBaseInterface(reader.ReadInt32());
        }

        public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
        {
            writer.WriteObject((TestReadableNameBaseInterface)obj);
        }

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(Index);
        }

        public bool Equals(TestReadableNameBaseInterface? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Index == other.Index;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((TestReadableNameBaseInterface)obj);
        }

        public override int GetHashCode()
        {
            return Index;
        }
    }
}
