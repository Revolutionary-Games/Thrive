using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MicrobeSpeciesTests
{
    private const string TestSpecies1 = @"
        {
            ""$id"": ""1"",
            ""$type"": ""MicrobeSpecies, Thrive"",
            ""IsBacteria"": true,
            ""MembraneType"": ""single"",
            ""MembraneRigidity"": 0,
            ""BaseRotationSpeed"": 0.18256894,
            ""InitialCompounds"": {
                ""Glucose"": 1.4399999,
                ""Iron"": 2
            },
            ""ID"": 69,
            ""Genus"": ""Cyiyes"",
            ""Epithet"": ""stex"",
            ""Colour"": {
                ""r"": 1,
                ""g"": 0.7509851,
                ""b"": 1,
                ""a"": 1
            },
            ""Obsolete"": false,
            ""Behaviour"": {
                ""Aggression"": 100,
                ""Opportunism"": 100,
                ""Fear"": 100,
                ""Activity"": 100,
                ""Focus"": 100
            },
            ""Population"": 438,
            ""Generation"": 1,
            ""PlayerSpecies"": false,
            ""Endosymbiosis"": {
                ""EngulfedSpecies"": {}
            },
            ""Organelles"": {
                ""existingHexes"": [
                    {
                        ""$id"": ""2"",
                        ""Definition"": ""cytoplasm"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": -3
                        },
                        ""Orientation"": 0
                    },
                    {
                        ""$id"": ""3"",
                        ""Definition"": ""flagellum"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": -2
                        },
                        ""Orientation"": 3,
                        ""Upgrades"": {
                            ""UnlockedFeatures"": [],
                            ""CustomUpgradeData"": {
                                ""$type"": ""FlagellumUpgrades, Thrive"",
                                ""LengthFraction"": -1
                            }
                        }
                    },
                    {
                        ""$id"": ""4"",
                        ""Definition"": ""nitrogenase"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": -1
                        },
                        ""Orientation"": 0
                    },
                    {
                        ""$id"": ""5"",
                        ""Definition"": ""nitrogenase"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": 0
                        },
                        ""Orientation"": 0
                    },
                    {
                        ""$id"": ""6"",
                        ""Definition"": ""cytoplasm"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": 1
                        },
                        ""Orientation"": 0
                    },
                    {
                        ""$id"": ""7"",
                        ""Definition"": ""chromatophore"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": 2
                        },
                        ""Orientation"": 0
                    },
                    {
                        ""$id"": ""8"",
                        ""Definition"": ""rusticyanin"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": 3
                        },
                        ""Orientation"": 0
                    }
                ]
            }
        }";

    private const string TestSpecies2 = @"
        {
            ""$id"": ""1"",
            ""$type"": ""MicrobeSpecies, Thrive"",
            ""IsBacteria"": true,
            ""MembraneType"": ""single"",
            ""MembraneRigidity"": 0,
            ""BaseRotationSpeed"": 0.10000003,
            ""InitialCompounds"": {
                ""Glucose"": 0.5
            },
            ""ID"": 1,
            ""Genus"": ""Primum"",
            ""Epithet"": ""thrivium"",
            ""Colour"": {
                ""r"": 1,
                ""g"": 1,
                ""b"": 1,
                ""a"": 1
            },
            ""Obsolete"": false,
            ""Behaviour"": {
                ""Aggression"": 100,
                ""Opportunism"": 100,
                ""Fear"": 100,
                ""Activity"": 100,
                ""Focus"": 100
            },
            ""Population"": 1044,
            ""Generation"": 3,
            ""PlayerSpecies"": true,
            ""Endosymbiosis"": {
                ""EngulfedSpecies"": {}
            },
            ""Organelles"": {
                ""existingHexes"": [
                    {
                        ""$id"": ""2"",
                        ""Definition"": ""cytoplasm"",
                        ""Position"": {
                            ""Q"": 0,
                            ""R"": 0
                        },
                        ""Orientation"": 0
                    }
                ]
            }
        }";

    [TestCase]
    public void DeserializedHashIsConsistent()
    {
        const ulong desiredHash = 680356391097977883;

        var species = ThriveJsonConverter.Instance.DeserializeObject<MicrobeSpecies>(TestSpecies1);

        AssertThat(species).IsNotNull();

        AssertThat(species!.GetVisualHashCode()).IsEqual(desiredHash);

        var hexesHash = CellHexesPhotoBuilder.GetVisualHash(species);

        AssertThat(hexesHash).IsNotEqual(desiredHash).IsNotEqual(species.GetVisualHashCode())
            .IsEqual(6703374232947739281);

        // Check that result has not changed
        AssertThat(species.GetVisualHashCode()).IsEqual(desiredHash);
    }

    [TestCase]
    public void SpecificSpeciesHashIsConsistent()
    {
        var species = ThriveJsonConverter.Instance.DeserializeObject<MicrobeSpecies>(TestSpecies2);
        AssertThat(species).IsNotNull();

        AssertThat(species!.GetVisualHashCode()).IsEqual(3818464758798215569);
    }
}
