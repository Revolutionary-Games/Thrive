#pragma once

#include <array>
#include <json/json.h>

#define LENGTH_OF_ARRAYS 50
#define NUMBER_OF_TESTS 100 // number of different planetary locations to test

class Star { // : public Leviathan::PerWorldData{

public:
    // star properties
    double starMass;
    double lifeSpan;
    double luminosity;
    double radius;
    double temperature;
    std::array<double, LENGTH_OF_ARRAYS> stellarSpectrum;
    double minOrbitalDiameter;
    double maxOrbitalDiameter;
    std::array<double, NUMBER_OF_TESTS> orbitalDistances;
    std::array<double, NUMBER_OF_TESTS> habitabilityScore;
    double gravitationalParameter;

    Star()
    {
        generateProperties(0);
    }

    void
        setSol()
    {
        starMass = 1.0; // solar masses
        generateProperties(1);
    }

    void
        setMass(double mass)
    {
        starMass = mass;
        generateProperties(1);
    }

    //! print the properties of the star
    void
        print();
    void
        printVerbose();

    //! update the star each turn
    void
        update();

private:
    //! set all the properties of the star, if step == 0 mass will be
    //! randomised, if step == 1 it will not be
    void
        generateProperties(int step);

    void
        setLifeSpan();
    void
        setLuminosity();
    void
        setRadius();
    void
        setTemperature();
    void
        setStellarSpectrum();
    void
        computeHabitableZone();
};

class Planet { // : public Leviathan::PerWorldData{

public:
    // planet properties
    Star* orbitingStar;
    double orbitalRadius;
    double planetRadius;
    double planetMass;
    double planetOrbitalPeriod;
    double lithosphereMass;
    double atmosphereMass;
    double oceanMass;
    double atmosphereWater;
    double atmosphereCarbonDioxide;
    double atmosphereOxygen;
    double atmosphereNitrogen;
    std::array<double, LENGTH_OF_ARRAYS> atmosphericFilter;
    std::array<double, LENGTH_OF_ARRAYS> terrestrialSpectrum;
    double planetTemperature;

    Planet(Star* star)
    {
        orbitingStar = star;
        generatePropertiesOrbitalRadius(0);
        generatePropertiesPlanetRadius(0);
        generatePropertiesAtmosphere(0);
    }

    void
        setEarth();

    void
        print();
    void
        printVerbose();

    void
        update();

    Json::Value
        toJSON() const;

private:
    void
        generatePropertiesOrbitalRadius(int step);
    void
        computeOptimalOrbitalRadius();
    void
        setPlanetPeriod();

    void
        generatePropertiesPlanetRadius(int step);
    void
        setoSphereMasses();
    void
        setPlanetMass();

    void
        generatePropertiesAtmosphere(int step);
    void
        setAtmosphereConstituentsRandom();
    void
        setAtmosphereConstituentsEarth();
    void
        massOfGasToClimateParameter(float& Oxygen, float& CarbonDioxide);
    void
        setPlanetTemperature();
    double
        computeSurfaceAreaFromRadius();
    double
        massOfGasIn1sqm(double MassOfGas);
    double
        atomsOfGasIn1sqm(double massOfGas, double molecularMass);
    double
        attenuationParameter(char gas);
    void
        computeLightFilter();
};
