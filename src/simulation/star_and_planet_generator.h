#pragma once

#include <array>
#include <json/json.h>
#include <memory>

#define LENGTH_OF_ARRAYS 50
#define NUMBER_OF_TESTS 100 // number of different planetary locations to test

class CelestialBody {

public:
    // properties
    std::shared_ptr<CelestialBody> orbitingBody;
    double orbitalRadius;
    double orbitalPeriod;
    // More orbital parameters like eccentricity inclination...?
    double mass;
    double radius;
    double gravitationalParameter;

protected:
    void
        setOrbitalPeriod();

    Json::Value
        toJSON() const;
};

class Star : public CelestialBody { // : public Leviathan::PerWorldData{

public:
    // star properties
    double lifeSpan;
    double luminosity;
    double temperature;
    std::array<double, LENGTH_OF_ARRAYS> stellarSpectrum;
    double minOrbitalDiameter;
    double maxOrbitalDiameter;
    std::array<double, NUMBER_OF_TESTS> orbitalDistances;
    std::array<double, NUMBER_OF_TESTS> habitabilityScore;

    Star()
    {
        generateProperties(0);
    }

    // start of the functions the player can call from the gui
    void
        setSol();
    void
        setMass(double newMass)
    {
        mass = newMass;
        generateProperties(1);
    }
    // end

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

    Json::Value
        toJSON() const;
};

class Planet : public CelestialBody { // : public Leviathan::PerWorldData{

public:
    // planet properties
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

    // Limit to star for now
    Planet(std::shared_ptr<Star> star)
    {
        orbitingBody = star;
        generatePropertiesOrbitalRadius(0);
        generatePropertiesPlanetMass(0);
        generatePropertiesAtmosphere(0);
    }

    // start of the functions the player can call from the gui
    void
        setEarth();
    void
        setOrbitalRadius(double newRadius)
    {
        orbitalRadius = radius;
        generatePropertiesOrbitalRadius(1);
    }
    void
        setPlanetMass(double newMass)
    {
        mass = newMass;
        generatePropertiesPlanetMass(1);
    }
    // end

    void
        print();
    void
        printVerbose();

    void
        update();

    std::string
        toJSONString() const;

private:
    void
        generatePropertiesOrbitalRadius(int step);
    void
        computeOptimalOrbitalRadius();

    void
        generatePropertiesPlanetMass(int step);
    void
        setSphereMasses();
    void
        setPlanetRadius();

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

    Json::Value
        toJSON() const;
};
