using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MicrobeSpeciesTests
{
    private const string TestSpecies1 = @"
                                        {
                                          ""$id"" : ""1"",
                                          ""$type"" : ""MicrobeSpecies, Thrive"",
                                          ""IsBacteria"" : true,
                                          ""MembraneType"" : ""single"",
                                          ""MembraneRigidity"" : 0.0,
                                          ""BaseRotationSpeed"" : 0.18256894,
                                          ""InitialCompounds"" : {
                                            ""Glucose"" : 1.4399999,
                                            ""Iron"" : 2.0
                                          },
                                          ""ID"" : 69,
                                          ""Genus"" : ""Cyiyes"",
                                          ""Epithet"" : ""stex"",
                                          ""Colour"" : {
                                            ""r"" : 1.0,
                                            ""g"" : 0.7509851,
                                            ""b"" : 1.0,
                                            ""a"" : 1.0
                                          },
                                          ""Obsolete"" : false,
                                          ""Behaviour"" : {
                                            ""Aggression"" : 100.0,
                                            ""Opportunism"" : 100.0,
                                            ""Fear"" : 100.0,
                                            ""Activity"" : 100.0,
                                            ""Focus"" : 100.0
                                          },
                                          ""Population"" : 438,
                                          ""Generation"" : 1,
                                          ""PlayerSpecies"" : false,
                                          ""Endosymbiosis"" : {
                                            ""EngulfedSpecies"" : { }
                                          },
                                          ""Organelles"" : {
                                            ""existingHexes"" : [ {
                                              ""$id"" : ""2"",
                                              ""Definition"" : ""cytoplasm"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : -3
                                              },
                                              ""Orientation"" : 0
                                            }, {
                                              ""$id"" : ""3"",
                                              ""Definition"" : ""flagellum"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : -2
                                              },
                                              ""Orientation"" : 3,
                                              ""Upgrades"" : {
                                                ""UnlockedFeatures"" : [ ],
                                                ""CustomUpgradeData"" : {
                                                  ""$type"" : ""FlagellumUpgrades, Thrive"",
                                                  ""LengthFraction"" : -1.0
                                                }
                                              }
                                            }, {
                                              ""$id"" : ""4"",
                                              ""Definition"" : ""nitrogenase"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : -1
                                              },
                                              ""Orientation"" : 0
                                            }, {
                                              ""$id"" : ""5"",
                                              ""Definition"" : ""nitrogenase"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : 0
                                              },
                                              ""Orientation"" : 0
                                            }, {
                                              ""$id"" : ""6"",
                                              ""Definition"" : ""cytoplasm"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : 1
                                              },
                                              ""Orientation"" : 0
                                            }, {
                                              ""$id"" : ""7"",
                                              ""Definition"" : ""chromatophore"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : 2
                                              },
                                              ""Orientation"" : 0
                                            }, {
                                              ""$id"" : ""8"",
                                              ""Definition"" : ""rusticyanin"",
                                              ""Position"" : {
                                                ""Q"" : 0,
                                                ""R"" : 3
                                              },
                                              ""Orientation"" : 0
                                            } ]
                                          }
                                        }
                                        ";

    [TestCase]
    public void DeserializedHashIsConsistent()
    {
        var species = ThriveJsonConverter.Instance.DeserializeObject<MicrobeSpecies>(TestSpecies1);

        AssertThat(species).IsNotNull();

        AssertThat(species!.GetVisualHashCode()).IsEqual(14021167612773540574);

        var hexesHash = CellHexesPhotoBuilder.GetVisualHash(species);

        AssertThat(hexesHash).IsNotEqual(species.GetVisualHashCode()).IsEqual(10872541043048358996);
    }
}
