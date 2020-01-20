#pragma once

#include <Common/ReferenceCounted.h>

#include <array>
#include <json/json.h>
#include <memory>

#define LENGTH_OF_ARRAYS 50
#define NUMBER_OF_TESTS 100 // number of different planetary locations to test

namespace thrive {

enum CelestialBodyType { STAR, PLANET };

class CelestialBody : public Leviathan::ReferenceCounted {
protected:
    friend ReferenceCounted;

public:
    REFERENCE_COUNTED_PTR_TYPE(CelestialBody);
    // properties
    CelestialBody::pointer orbitingBody;
    CelestialBodyType celestialBodyType;
    double orbitalRadius = 0;
    double orbitalPeriod = 0;
    // More orbital parameters like eccentricity inclination...?
    double mass = 0;
    double radius = 0;
    double gravitationalParameter = 0;

    Json::Value
        toJSON() const;

protected:
    void
        setOrbitalPeriod();
};

class Star : public CelestialBody { // : public Leviathan::PerWorldData{

public:
    // star properties
    double lifeSpan = 0;
    double luminosity = 0;
    double temperature = 0;
    std::array<double, LENGTH_OF_ARRAYS> stellarSpectrum;
    double minOrbitalDiameter = 0;
    double maxOrbitalDiameter = 0;
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

    Json::Value
        toJSON() const;

    REFERENCE_COUNTED_PTR_TYPE(Star);

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

class Planet : public CelestialBody { // : public Leviathan::PerWorldData{

public:
    // planet properties
    double lithosphereMass = 0;
    double atmosphereMass = 0;
    double oceanMass = 0;
    double atmosphereWater = 0;
    double atmosphereCarbonDioxide = 0;
    double atmosphereOxygen = 0;
    double atmosphereNitrogen = 0;
    std::array<double, LENGTH_OF_ARRAYS> atmosphericFilter;
    std::array<double, LENGTH_OF_ARRAYS> terrestrialSpectrum;
    double planetTemperature = 0;
    int habitability = 0;
    // where to draw the current orbital radius on the habitavility graph
    double orbitalRadiusGraphFraction = 0;

    // Limit to star for now (may eventually want to allow binary planetary
    // systems, though that would need a "Barycenter" CelestialBody)
    Planet(Star::pointer star)
    {
        orbitingBody = star;
        orbitingBody->celestialBodyType = STAR;
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
        orbitalRadius = newRadius;
        generatePropertiesOrbitalRadius(1);
        generatePropertiesAtmosphere(1);
    }
    void
        setPlanetMass(double newMass)
    {
        mass = newMass;
        generatePropertiesPlanetMass(1);
        generatePropertiesOrbitalRadius(0);
        generatePropertiesAtmosphere(0);
    }
    void
        setOxygen(double percentageAtmosphereOxygen);
    void
        setCarbonDioxide(double percentageAtmosphereCarbonDioxide);
    // end

    void
        print();
    void
        printVerbose();

    void
        update();

    Json::Value
        toJSON() const;

    std::string
        toJSONString() const;

    REFERENCE_COUNTED_PTR_TYPE(Planet);

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
        setPlanetMass();
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
};

} // namespace thrive
