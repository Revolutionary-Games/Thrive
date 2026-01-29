using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using SharedBase.Archive;

public class ReproductionStatistic : IStatistic, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public int TimesReproduced { get; private set; }

    public Dictionary<Biome, int> ReproducedInBiomes { get; private set; } = new();

    public Dictionary<OrganelleDefinition, ReproductionOrganelleData> ReproducedWithOrganelle { get; private set; } =
        new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ReproductionStatistic;

    public void RecordPlayerReproduction(in Entity player, Biome biome)
    {
        ++TimesReproduced;

        // Due to needing to track how many generations in a row organelle was in the player's species, all organelles
        // (even ones that aren't currently added) need to be processed
        foreach (var definition in SimulationParameters.Instance.GetAllOrganelles())
        {
            if (!ReproducedWithOrganelle.TryGetValue(definition, out var data))
            {
                data = ReproducedWithOrganelle[definition] = new ReproductionOrganelleData();
            }

            data.IncrementBy(CountOrganellesOfType(definition, player));
        }

        if (ReproducedInBiomes.TryGetValue(biome, out var value))
        {
            ReproducedInBiomes[biome] = ++value;
        }
        else
        {
            ReproducedInBiomes[biome] = 1;
        }
    }

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(TimesReproduced);
        writer.WriteObject(ReproducedInBiomes);
        writer.WriteObject(ReproducedWithOrganelle);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        TimesReproduced = reader.ReadInt32();
        ReproducedInBiomes = reader.ReadObject<Dictionary<Biome, int>>();
        ReproducedWithOrganelle = reader.ReadObject<Dictionary<OrganelleDefinition, ReproductionOrganelleData>>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CountOrganellesOfType(OrganelleDefinition definition, in Entity player)
    {
        int count = 0;

        foreach (var organelle in player.Get<OrganelleContainer>().Organelles!.Organelles)
        {
            // As the player grows before reproducing, this might end up doubly counting organelles, so this skip is
            // here
            if (organelle.IsDuplicate)
                continue;

            if (organelle.Definition == definition)
                ++count;
        }

        if (player.TryGet<MicrobeColony>(out var colony))
        {
            var members = colony.ColonyMembers;

            for (int i = 1; i < members.Length; ++i)
            {
                foreach (var organelle in members[i].Get<OrganelleContainer>().Organelles!.Organelles)
                {
                    if (organelle.Definition == definition)
                    {
                        ++count;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    ///   Contains data about how many times the player has reproduced with an organelle
    /// </summary>
    public class ReproductionOrganelleData : IArchivable
    {
        public const ushort SERIALIZATION_VERSION_ORGANELLE = 1;

        /// <summary>
        ///   The total number of generations the player evolved with this organelle
        /// </summary>
        public int TotalGenerations { get; private set; }

        /// <summary>
        ///   The number of generations that the player evolved this organelle in a row
        /// </summary>
        public int GenerationsInARow { get; private set; }

        /// <summary>
        ///   The amount of this organelle the player evolved with in each generation
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: some kind of upper limit might be nice here to not just add more and more data that accumulates
        ///     here
        ///   </para>
        /// </remarks>
        public List<int> CountInGenerations { get; private set; } = new();

        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION_ORGANELLE;

        public ArchiveObjectType ArchiveObjectType =>
            (ArchiveObjectType)ThriveArchiveObjectType.ReproductionOrganelleData;

        public bool CanBeReferencedInArchive => false; // Not for ArchiveUpdatable

        public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
        {
            if (type != (ArchiveObjectType)ThriveArchiveObjectType.ReproductionOrganelleData)
                throw new NotSupportedException();

            writer.WriteObject((ReproductionOrganelleData)obj);
        }

        public static ReproductionOrganelleData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
        {
            if (version is > SERIALIZATION_VERSION_ORGANELLE or <= 0)
                throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_ORGANELLE);

            var instance = new ReproductionOrganelleData
            {
                TotalGenerations = reader.ReadInt32(),
                GenerationsInARow = reader.ReadInt32(),
                CountInGenerations = reader.ReadObject<List<int>>(),
            };

            return instance;
        }

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(TotalGenerations);
            writer.Write(GenerationsInARow);
            writer.WriteObject(CountInGenerations);
        }

        public void IncrementBy(int count)
        {
            if (count <= 0)
            {
                // A generation without this organelle
                GenerationsInARow = 0;
                CountInGenerations.Add(0);
                return;
            }

            CountInGenerations.Add(count);
            ++GenerationsInARow;
            ++TotalGenerations;
        }
    }
}
