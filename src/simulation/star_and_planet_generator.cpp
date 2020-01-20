#include "star_and_planet_generator.h"
#include "ThriveGame.h"

#include <iostream>
#include <math.h>
#include <random>
#include <stdio.h>
#include <time.h>

using namespace thrive;

// ------------------------------------ //
// Constants
// ------------------------------------ //


constexpr double GRAVITATIONAL_CONSTANT = 6.674e-11; // Newtons Meters^2 / kg^2
constexpr double LUMINOSITY_OF_OUR_SUN = 3.846e26; // watts
constexpr double MASS_OF_OUR_SUN = 1.989e30; // kg
constexpr double RADIUS_OF_OUR_SUN = 6.96e8; // meters
constexpr double RADIUS_OF_THE_EARTH = 6.371e6; // meters
constexpr double MASS_OF_THE_EARTH = 5.972e24; // kg
constexpr double STEPHAN = 5.67e-8; // Watts meters^-2 Kelvin^-4 constant
constexpr double PI = 3.14159265358979323846;
constexpr double HC = 1.98e-25; // plank's constant times speed of light
constexpr double HC2 =
    5.95e-17; // plank's constant times speed of light squared
constexpr double KB = 1.38e-23; // Boltzmanns constant
constexpr double WAVELENGTH_STEP =
    5e-8; // 0.05 microns per step, there are 50 steps so this is 2.5 microns.
constexpr double E = 2.71828182845904523536; // you all know and love it!
// visible spectrum for humans is 0.38 - 0.75 microns
constexpr double SMALL_DELTA =
    0.01; // step size for the climate differential equation
constexpr double ALBEDO = 0.65; // base albedo value (planetary reflectivity)
constexpr double BASE_MAX_ORBITAL_DIAMETER =
    7.78e11; // meters (radius of jupiter)
constexpr double BASE_MIN_ORBITAL_DIAMETER =
    5.5e10; // meters (radius of mercury)
constexpr double OXYGEN_PARAMETER =
    0.3; // amount of sunlight ozone can block if atmosphere is 100% oxygen
constexpr double CARBON_DIOXIODE_PARAMETER = 0.3; // same
constexpr double WATER_VAPOUR_PARAMETER = 0.3; // same
constexpr double NUMBER_OF_GAS_CHECKS =
    9; // number of different values of CO2 and O2 to check, more is better but
       // very intensive
constexpr double MIN_PLANET_RADIUS =
    5375699.0; // smallest radius allowed, see
               // http://forum.revolutionarygamesstudio.com/t/planet-generation/182/10
constexpr double MAX_PLANET_RADIUS = 9191080.0; // largest radius allowed
constexpr double MIN_PLANET_MASS = 1.5528927539e24;
constexpr double MAX_PLANET_MASS = 1.79373143543e25;
constexpr double DENSITY_OF_EARTH =
    5515.3; // kg m^-3 assume all planets are the same density as earth
constexpr double PERCENTAGE_ATMOSPHERE =
    8.62357669e-7; // percentage of the earths mass which is atmosphere
constexpr double PERCENTAGE_OCEAN = 2.26054923e-7; // percentage that is ocean
constexpr double PERCENTAGE_LITHOSPHERE =
    1.67448091e-7; // percentage that is rock, just a guess in line with others
constexpr double FUDGE_FACTOR_NITROGEN =
    7.28704114055e-10; // calibrate the spectral computations using earths
                       // atmosphere
constexpr double FUDGE_FACTOR_WATER = 6.34362956432e-10; // same, was e-09
constexpr double FUDGE_FACTOR_CARBON_DIOXIDE = 1.55066500461e-08; // same
constexpr double FUDGE_FACTOR_OXYGEN = 3.42834549545e-10; // same, was e-09
constexpr double AVOGADRO =
    6.022e23; // avagadros constant relating number of atoms to mass
constexpr double MOLECULAR_MASS_CARBON_DIOXIDE =
    0.044; // kg mol^-1, mass of 1 mole of CO2
constexpr double MOLECULAR_MASS_OXYGEN =
    0.032; //  kg mol^-1, mass of 1 mole of O2
constexpr double MOLECULAR_MASS_NITROGEN =
    0.028; //  kg mol^-1, mass of 1 mole of N2
constexpr double MOLECULAR_MASS_WATER =
    0.018; //  kg mol^-1, mass of 1 mole of H2O
constexpr double DIAMETER_WATER =
    9.0e-11; // meters, size of a water molecule for interaction with light
constexpr double DIAMETER_NITROGEN = 7.5e-11; //  meters
constexpr double DIAMETER_CARBON_DIOXIDE = 9e-11; // meters
constexpr double DIAMETER_OXYGEN = 7.3e-11; // meters
constexpr double EARTH_OXYGEN_MASS = 1.03038e+018; // kg
constexpr double EARTH_CARBON_DIOXIDE_MASS = 2.06076e+017; // kg

// ------------------------------------ //
// Utility Functions
// ------------------------------------ //

// 4 functions for computing the temperature based on sunlight, CO2 and O2
// work out how reflective the planet is
float
    computeAlbedo(float temperature)
{
    if(temperature < 273) {
        return 0.7;
    }
    if(temperature > 373) {
        return 0.6;
    } else {
        return 0.7 - (0.1 * (temperature - 273) / 100);
    }
}

// compute the warming effect from water vapour in the atmosphere
float
    computeWaterVapour(float temperature)
{
    if(temperature < 273) {
        return 0;
    }
    if(temperature > 373) {
        return 1;
    } else {
        return (temperature - 273) / 100;
    }
}

// compute the temperature change (dT/dt)
float
    computeTempChange(double incomingSunlight,
        float carbonDioxide,
        float oxygen,
        float waterVapour,
        float albedo,
        float temperature)
{
    return ((1 - albedo) * (1 - oxygen * OXYGEN_PARAMETER) * incomingSunlight -
            (1 - waterVapour * WATER_VAPOUR_PARAMETER) *
                (1 - carbonDioxide * CARBON_DIOXIODE_PARAMETER) * STEPHAN *
                pow(temperature, 4));
}

// compute the temerature by running the ODE to an equilibrium
float
    computeTemperature(double incomingSunlight,
        float carbonDioxide,
        float oxygen)
{
    float temperature = 200.0;
    for(int i = 0; i < 1000; i++) {
        float waterVapour = computeWaterVapour(temperature);
        float albedo = computeAlbedo(temperature);
        temperature += computeTempChange(incomingSunlight, carbonDioxide,
                           oxygen, waterVapour, albedo, temperature) *
                       SMALL_DELTA;
    }
    return temperature;
}

// compute the habiltability score for a given amount of incoming sunlight
int
    computeHabitabilityScore(double incomingSunlight)
{
    int habitability = 0;
    for(int i = 0; i <= NUMBER_OF_GAS_CHECKS; i++) {
        float carbonDioxide = i * ((float)1 / (float)NUMBER_OF_GAS_CHECKS);
        for(int j = 0; j <= NUMBER_OF_GAS_CHECKS; j++) {
            float oxygen = j * ((float)1 / (float)NUMBER_OF_GAS_CHECKS);
            float temp =
                computeTemperature(incomingSunlight, carbonDioxide, oxygen);
            ;
            if(temp < 373 && temp > 273) {
                habitability++;
            }
        }
    }
    return habitability;
}

// generate a random real number between two bounds
double
    fRand(double fMin, double fMax)
{
    std::random_device random_device;
    std::mt19937 random_engine(random_device());
    std::uniform_real_distribution<double> distribution(fMin, fMax);
    return distribution(random_engine);
}

// multiply two spectra together to get a 3rd
void
    multiplyArrays(const std::array<double, LENGTH_OF_ARRAYS>& Array1,
        const std::array<double, LENGTH_OF_ARRAYS>& Array2,
        std::array<double, LENGTH_OF_ARRAYS>& target)
{
    for(int i = 0; i < LENGTH_OF_ARRAYS; i++) {
        target.at(i) = Array1.at(i) * Array2.at(i);
    }
}

double
    planksLaw(int temperature, double wavelength)
{
    double partial = pow(E, (HC / (wavelength * KB * temperature))) - 1;
    return 2 * HC2 / (partial * (pow(wavelength, 5)));
}

void
    printSpectrum(const std::array<double, LENGTH_OF_ARRAYS>& Array1)
{
    LOG_INFO("Wavelength meters : Energy Watts");
    for(int i = 0; i < LENGTH_OF_ARRAYS; i++) {
        LOG_INFO(Convert::ToString(WAVELENGTH_STEP * (i + 1)) + ": " +
                 Convert::ToString(Array1.at(i)));
    }
}

// ------------------------------------ //
// CelestialBody
// ------------------------------------ //

// compute the orbital period using Kepler's law
// https://en.wikipedia.org/wiki/Orbital_period
void
    CelestialBody::setOrbitalPeriod()
{
    if(orbitalRadius > 0 && orbitingBody &&
        orbitingBody->gravitationalParameter > 0) {
        orbitalPeriod = 2 * PI *
                        pow(((pow(orbitalRadius, 3)) /
                                orbitingBody->gravitationalParameter),
                            0.5);
    } else {
        orbitalPeriod = 0;
    }
}

Json::Value
    CelestialBody::toJSON() const
{
    Json::Value result;

    // don't know if recursiveness here might be bad...
    if(orbitingBody) {
        if(orbitingBody->celestialBodyType == STAR) {
            Star::pointer orbitingStar =
                boost::static_pointer_cast<Star>(orbitingBody);
            result["orbitingBody"] = orbitingStar->toJSON();
        } else if(orbitingBody->celestialBodyType == PLANET) {
            Planet::pointer orbitingPlanet =
                boost::static_pointer_cast<Planet>(orbitingBody);
            result["orbitingBody"] = orbitingPlanet->toJSON();
        } else {
            result["orbitingBody"] = orbitingBody->toJSON();
        }
    }

    Json::Value orbit;
    orbit["radius"] = orbitalRadius;
    orbit["period"] = orbitalPeriod;

    result["orbit"] = orbit;
    result["mass"] = mass;
    result["radius"] = radius;
    result["gravitationalParameter"] = gravitationalParameter;

    return result;
}


// ------------------------------------ //
// Star
// ------------------------------------ //

void
    Star::setSol()
{
    mass = MASS_OF_OUR_SUN;
    generateProperties(1);
}

void
    Star::generateProperties(int step)
{
    if(step <= 0) {
        // randomly choose the mass of the star
        mass = fRand(0.5, 3) * MASS_OF_OUR_SUN; // kilograms
    }
    if(step <= 1) {
        // compute the other variables like lifespan, luminosity etc
        setLifeSpan();
        setLuminosity();
        setRadius();
        setTemperature();
        setStellarSpectrum();
        minOrbitalDiameter =
            (mass / MASS_OF_OUR_SUN) * BASE_MIN_ORBITAL_DIAMETER;
        maxOrbitalDiameter =
            (mass / MASS_OF_OUR_SUN) * BASE_MAX_ORBITAL_DIAMETER;
        computeHabitableZone();
        gravitationalParameter = GRAVITATIONAL_CONSTANT * mass;
    }
}

void
    Star::print()
{
    // these would all be replaced with log info's
    LOG_INFO("The Star.");
    LOG_INFO("Mass: " + Convert::ToString(mass) + " kg = " +
             Convert::ToString(mass / MASS_OF_OUR_SUN) + " Solar Masses.");
    LOG_INFO("Life Span: " + Convert::ToString(lifeSpan) + " of our years.");
    LOG_INFO("Luminosity: " + Convert::ToString(luminosity) + " watts.");
    LOG_INFO("Radius: " + Convert::ToString(radius) + " meters.");
    LOG_INFO("Temperature: " + Convert::ToString(temperature) + " Kelvin.");
}

void
    Star::printVerbose()
{

    print();
    LOG_INFO("Stellar Spectrum.");
    printSpectrum(stellarSpectrum);
    LOG_INFO("Habitability Scores in form Radius(m): Habitability Score.");
    for(int i = 0; i < NUMBER_OF_TESTS; i++) {
        LOG_INFO(Convert::ToString(orbitalDistances.at(i)) + " : " +
                 Convert::ToString(habitabilityScore.at(i)));
    }
}

// from wikipedia table
// https://en.wikipedia.org/wiki/File:Representative_lifetimes_of_stars_as_a_function_of_their_masses.svg
void
    Star::setLifeSpan()
{
    lifeSpan = 1e10 / (pow(mass / MASS_OF_OUR_SUN, 3));
}

// from wikipedia https://en.wikipedia.org/wiki/Mass%E2%80%93luminosity_relation
void
    Star::setLuminosity()
{
    luminosity = LUMINOSITY_OF_OUR_SUN * (pow(mass / MASS_OF_OUR_SUN, 3.5));
}

// from (7.14c) of
// http://physics.ucsd.edu/students/courses/winter2008/managed/physics223/documents/Lecture7%13Part3.pdf
void
    Star::setRadius()
{
    radius = RADIUS_OF_OUR_SUN * (pow(mass / MASS_OF_OUR_SUN, 0.9));
}

// from the same page as luminosity using the formula for temperature and
// luminosity
void
    Star::setTemperature()
{
    temperature =
        pow((luminosity / (4 * PI * STEPHAN * (pow(radius, 2)))), 0.25);
}

void
    Star::setStellarSpectrum()
{
    for(int i = 0; i < LENGTH_OF_ARRAYS; i++) {
        stellarSpectrum.at(i) =
            planksLaw(temperature, WAVELENGTH_STEP * (i + 1));
    }
}

// compute how habitable a planet would be at different radii
void
    Star::computeHabitableZone()
{
    // start at a close distance
    double diameterStep =
        (maxOrbitalDiameter - minOrbitalDiameter) / NUMBER_OF_TESTS;
    for(int i = 0; i < NUMBER_OF_TESTS; i++) {
        double currentDiameter = minOrbitalDiameter + i * diameterStep;
        habitabilityScore.at(i) = 0;
        // work out incoming sunlight
        double incomingSunlight =
            luminosity / (4 * PI * (pow(currentDiameter, 2)));
        // test different values of CO2 and O2 in the atmosphere
        habitabilityScore.at(i) = computeHabitabilityScore(incomingSunlight);
        orbitalDistances.at(i) = currentDiameter;
    }
    // habitabilityScore.at(0) = 0; // fixing a weird bug I found, sorry :(
}

void
    Star::update()
{
    mass += MASS_OF_OUR_SUN;
}

Json::Value
    Star::toJSON() const
{
    Json::Value result = CelestialBody::toJSON();

    result["lifeSpan"] = lifeSpan;
    result["temperature"] = temperature;

    Json::Value stellarSpectrumJ(Json::ValueType::arrayValue);
    for(auto spectrum : stellarSpectrum)
        stellarSpectrumJ.append(spectrum);
    result["stellarSpectrum"] = stellarSpectrumJ;

    Json::Value habitabilityScoreJ(Json::ValueType::arrayValue);
    for(auto habitability : habitabilityScore)
        habitabilityScoreJ.append(habitability);
    result["habitabilityScore"] = habitabilityScoreJ;

    return result;
}

// ------------------------------------ //
// Planet
// ------------------------------------ //



void
    Planet::setEarth()
{
    mass = MASS_OF_THE_EARTH;
    generatePropertiesOrbitalRadius(0);
    generatePropertiesPlanetMass(1);
    setAtmosphereConstituentsEarth();
    generatePropertiesAtmosphere(1);
}

void
    Planet::generatePropertiesOrbitalRadius(int step)
{
    if(step <= 0) {
        computeOptimalOrbitalRadius();
    }
    if(step <= 1) {
        setOrbitalPeriod();
        // work out where in the habitability graph to draw the planet
        double minOrbitalDiameter =
            (orbitingBody->mass / MASS_OF_OUR_SUN) * BASE_MIN_ORBITAL_DIAMETER;
        double maxOrbitalDiameter =
            (orbitingBody->mass / MASS_OF_OUR_SUN) * BASE_MAX_ORBITAL_DIAMETER;
        orbitalRadiusGraphFraction = (orbitalRadius - minOrbitalDiameter) /
                                     (maxOrbitalDiameter - minOrbitalDiameter);
    }
}

// put the planet in the optimal place in the system
void
    Planet::computeOptimalOrbitalRadius()
{
    Star::pointer orbitingStar = boost::static_pointer_cast<Star>(orbitingBody);
    int maxHabitability = 0;
    int entryLocation = 0;
    for(int i = 0; i < NUMBER_OF_TESTS; i++) {
        if(orbitingStar->habitabilityScore.at(i) > maxHabitability) {
            maxHabitability = orbitingStar->habitabilityScore.at(i);
            entryLocation = i;
        }
    }
    orbitalRadius = orbitingStar->orbitalDistances.at(entryLocation);
}

void
    Planet::generatePropertiesPlanetMass(int step)
{
    if(step <= 0) {
        mass = fRand(MIN_PLANET_MASS, MAX_PLANET_MASS);
    }
    if(step <= 1) {
        setPlanetRadius();
        setSphereMasses();
    }
}

void
    Planet::setPlanetRadius()
{
    radius = std::cbrt(3 * mass / (DENSITY_OF_EARTH * 4 * PI));
}

// set the masses of the atmosphere, ocean and lithosphere
void
    Planet::setSphereMasses()
{
    atmosphereMass = mass * PERCENTAGE_ATMOSPHERE;
    oceanMass = mass * PERCENTAGE_OCEAN;
    lithosphereMass = mass * PERCENTAGE_LITHOSPHERE;
}

void
    Planet::generatePropertiesAtmosphere(int step)
{
    if(step <= 0) {
        setAtmosphereConstituentsRandom();
    }
    if(step <= 1) {
        // compute the habitability of the planet
        Star::pointer orbitingStar =
            boost::static_pointer_cast<Star>(orbitingBody);
        double incomingSunlight =
            orbitingStar->luminosity / (4 * PI * (pow(orbitalRadius, 2)));
        habitability = computeHabitabilityScore(incomingSunlight);
        setPlanetTemperature();
        computeLightFilter();
        multiplyArrays(orbitingStar->stellarSpectrum, atmosphericFilter,
            terrestrialSpectrum);
    }
}

// choose what gasses to have in your atmosphere.
void
    Planet::setAtmosphereConstituentsRandom()
{
    double currentPercentage = fRand(0, 0.9);
    atmosphereOxygen = atmosphereMass * currentPercentage;
    double newRange = 1 - currentPercentage;
    currentPercentage = fRand(0, newRange);
    atmosphereCarbonDioxide = atmosphereMass * currentPercentage;
    newRange = newRange - currentPercentage;
    atmosphereNitrogen = atmosphereMass * newRange;
    atmosphereWater = atmosphereMass * 0.04;
}
void
    Planet::setAtmosphereConstituentsEarth()
{
    atmosphereOxygen = atmosphereMass * 0.2;
    atmosphereWater = atmosphereMass * 0.04;
    atmosphereCarbonDioxide = atmosphereMass * 0.04;
    atmosphereNitrogen = atmosphereMass * 0.72;
    generatePropertiesAtmosphere(1);
}

void
    Planet::setOxygen(double percentageAtmosphereOxygen)
{
    atmosphereOxygen = atmosphereMass * percentageAtmosphereOxygen;
    atmosphereCarbonDioxide = std::max(0.0,
        std::min(atmosphereCarbonDioxide, atmosphereMass - atmosphereOxygen));
    atmosphereNitrogen = std::max(
        0.0, atmosphereMass - atmosphereOxygen - atmosphereCarbonDioxide);
    atmosphereWater = atmosphereMass * 0.04;
    generatePropertiesAtmosphere(1);
}

void
    Planet::setCarbonDioxide(double percentageAtmosphereCarbonDioxide)
{
    atmosphereCarbonDioxide =
        atmosphereMass * percentageAtmosphereCarbonDioxide;
    atmosphereOxygen = std::max(0.0,
        std::min(atmosphereOxygen, atmosphereMass - atmosphereCarbonDioxide));
    atmosphereNitrogen = std::max(
        0.0, atmosphereMass - atmosphereOxygen - atmosphereCarbonDioxide);
    atmosphereWater = atmosphereMass * 0.04;
    generatePropertiesAtmosphere(1);
}

// compute the atmospheric parameters from the mass of gas
void
    Planet::massOfGasToClimateParameter(float& oxygen, float& carbonDioxide)
{
    if(atmosphereOxygen < (0.5 * EARTH_OXYGEN_MASS)) {
        oxygen = 0;
    } else if(atmosphereOxygen > (1.5 * EARTH_OXYGEN_MASS)) {
        oxygen = 1;
    } else {
        oxygen = (atmosphereOxygen / EARTH_OXYGEN_MASS) - 0.5;
    }
    if(atmosphereCarbonDioxide < (0.985 * EARTH_CARBON_DIOXIDE_MASS)) {
        carbonDioxide = 0;
    } else if(atmosphereCarbonDioxide > (1.985 * EARTH_CARBON_DIOXIDE_MASS)) {
        carbonDioxide = 1;
    } else {
        carbonDioxide =
            (atmosphereCarbonDioxide / EARTH_CARBON_DIOXIDE_MASS) - 0.985;
    }
}

// compute the final temperature of the planet
void
    Planet::setPlanetTemperature()
{
    // For now let's assume it's a star...
    double incomingSunlight =
        boost::static_pointer_cast<Star>(orbitingBody)->luminosity /
        (4 * PI * (pow(orbitalRadius, 2)));
    float oxygen;
    float carbonDioxide;
    massOfGasToClimateParameter(oxygen, carbonDioxide);
    planetTemperature =
        computeTemperature(incomingSunlight, carbonDioxide, oxygen);
}

// simple formula for surface area of a sphere
double
    Planet::computeSurfaceAreaFromRadius()
{
    return 4 * PI * (pow(radius, 2));
}

// how much gas is there in a column above 1sq meter of land?
double
    Planet::massOfGasIn1sqm(double massOfGas)
{
    double surfaceArea = computeSurfaceAreaFromRadius();
    double massIn1sqm = massOfGas / surfaceArea;
    return massIn1sqm;
}

// how many atoms are there in a column above 1sq m of land?
double
    Planet::atomsOfGasIn1sqm(double massOfGas, double molecularMass)
{
    double massIn1sqm = massOfGasIn1sqm(massOfGas);
    double numberOfMoles = massIn1sqm / molecularMass;
    double numberOfAtoms = numberOfMoles * AVOGADRO;
    return numberOfAtoms;
}

// what percentage of the light should make it through?
double
    Planet::attenuationParameter(char gas)
{
    double fudgeFactor;
    double molecularArea;
    double molecularMass;
    double massOfGas = 0;
    if(gas == 'w') {
        fudgeFactor = FUDGE_FACTOR_WATER;
        molecularArea = pow(DIAMETER_WATER, 2);
        molecularMass = MOLECULAR_MASS_WATER;
        massOfGas = atmosphereWater;
    }
    if(gas == 'o') {
        fudgeFactor = FUDGE_FACTOR_OXYGEN;
        molecularArea = pow(DIAMETER_OXYGEN, 2);
        molecularMass = MOLECULAR_MASS_OXYGEN;
        massOfGas = atmosphereOxygen;
    }
    if(gas == 'n') {
        fudgeFactor = FUDGE_FACTOR_NITROGEN;
        molecularArea = pow(DIAMETER_NITROGEN, 2);
        molecularMass = MOLECULAR_MASS_NITROGEN;
        massOfGas = atmosphereNitrogen;
    }
    if(gas == 'c') {
        fudgeFactor = FUDGE_FACTOR_CARBON_DIOXIDE;
        molecularArea = pow(DIAMETER_CARBON_DIOXIDE, 2);
        molecularMass = MOLECULAR_MASS_CARBON_DIOXIDE;
        massOfGas = atmosphereCarbonDioxide;
    }
    double numberOfAtoms = atomsOfGasIn1sqm(massOfGas, molecularMass);
    double exponent = -fudgeFactor * numberOfAtoms * molecularArea;
    return pow(E, exponent);
}

// compute how the atmospheric gasses filter the light
void
    Planet::computeLightFilter()
{
    // what percentage of light to block for different compounds?
    // this value is the base and on earth, for all of them, it should be 0.5
    // this base value is then, as below, taken to a power based on wavelength
    double water = attenuationParameter('w');
    double largestFilter = water;
    double nitrogen = attenuationParameter('n');
    if(nitrogen > largestFilter) {
        largestFilter = nitrogen;
    }
    double oxygen = attenuationParameter('o');
    if(oxygen > largestFilter) {
        largestFilter = oxygen;
    }
    double carbonDioxide = attenuationParameter('c');
    if(carbonDioxide > largestFilter) {
        largestFilter = carbonDioxide;
    }
    // define the values of the filter
    atmosphericFilter.at(0) = (pow(nitrogen, 0.3)) * (pow(oxygen, 2.2)) * water;
    atmosphericFilter.at(1) = (pow(nitrogen, 0.3)) * (pow(oxygen, 2.2)) * water;
    atmosphericFilter.at(2) = (pow(oxygen, 2.2)) * (pow(water, 2.2));
    atmosphericFilter.at(3) = (pow(oxygen, 2.2)) * (pow(water, 2.2));
    atmosphericFilter.at(4) = (pow(oxygen, 2.2));
    atmosphericFilter.at(5) = (pow(oxygen, 2.2));
    atmosphericFilter.at(6) = (pow(oxygen, 2.2));
    atmosphericFilter.at(7) = pow(oxygen, 1.7);
    atmosphericFilter.at(8) = pow(oxygen, 1.7);
    atmosphericFilter.at(9) = pow(oxygen, 1.7);
    atmosphericFilter.at(10) = pow(oxygen, 1.7);
    atmosphericFilter.at(11) = (pow(oxygen, 1.7)) * (pow(water, 2.2));
    atmosphericFilter.at(12) = (pow(oxygen, 1.7)) * (pow(water, 2.2));
    atmosphericFilter.at(13) = (pow(water, 2.2)) * (pow(oxygen, 1.7));
    atmosphericFilter.at(14) = (pow(oxygen, 1.7));
    atmosphericFilter.at(15) = (pow(water, 2.2)) * 1 * oxygen;
    atmosphericFilter.at(16) = (pow(oxygen, 1.7));
    atmosphericFilter.at(17) = largestFilter;
    atmosphericFilter.at(18) = (pow(water, 2.2));
    atmosphericFilter.at(19) = largestFilter;
    atmosphericFilter.at(20) = (pow(oxygen, 1.7));
    atmosphericFilter.at(21) = (pow(water, 2.2));
    atmosphericFilter.at(22) = largestFilter;
    atmosphericFilter.at(23) = largestFilter;
    atmosphericFilter.at(24) = (pow(oxygen, 1.7));
    atmosphericFilter.at(25) = largestFilter;
    atmosphericFilter.at(26) = 1 * water;
    atmosphericFilter.at(27) = pow(carbonDioxide, 0.75);
    atmosphericFilter.at(28) = largestFilter;
    atmosphericFilter.at(29) = pow(carbonDioxide, 2.3);
    atmosphericFilter.at(30) = largestFilter;
    atmosphericFilter.at(31) = (pow(oxygen, 1.7));
    atmosphericFilter.at(32) = largestFilter;
    atmosphericFilter.at(33) = largestFilter;
    atmosphericFilter.at(34) = largestFilter;
    atmosphericFilter.at(35) = largestFilter;
    atmosphericFilter.at(36) = 1 * water;
    atmosphericFilter.at(37) = largestFilter;
    atmosphericFilter.at(38) = largestFilter;
    atmosphericFilter.at(39) = 1 * carbonDioxide;
    atmosphericFilter.at(40) = largestFilter;
    atmosphericFilter.at(41) = largestFilter;
    atmosphericFilter.at(42) = largestFilter;
    atmosphericFilter.at(43) = largestFilter;
    atmosphericFilter.at(44) = largestFilter;
    atmosphericFilter.at(45) = largestFilter;
    atmosphericFilter.at(46) = largestFilter;
    atmosphericFilter.at(47) = (pow(water, 0.1));
    atmosphericFilter.at(48) = (pow(water, 0.1));
    atmosphericFilter.at(49) = largestFilter;
}

void
    Planet::print()
{
    // these would all be replaced with log info's
    LOG_INFO("Info about the current planet.");
    LOG_INFO("Orbital Radius: " + Convert::ToString(orbitalRadius) + " m.");
    LOG_INFO("Radius: " + Convert::ToString(radius) + " m.");
    LOG_INFO("Planet Mass: " + Convert::ToString(mass) + " kg.");
    LOG_INFO("Orbital Period: " + Convert::ToString(orbitalPeriod) +
             " seconds = " + Convert::ToString(orbitalPeriod / 3.154e+7) +
             " earth years.");
    LOG_INFO("Mass of atmosphere: " + Convert::ToString(atmosphereMass) +
             ", Mass of Ocean: " + Convert::ToString(oceanMass) +
             ", Mass of Lithosphere: " + Convert::ToString(lithosphereMass) +
             ", Mass of Atmosphere: " + Convert::ToString(atmosphereMass) +
             " kg.");
    LOG_INFO("Water: " + Convert::ToString(atmosphereWater) +
             ", Oxygen: " + Convert::ToString(atmosphereOxygen) +
             ", Nitrogen: " + Convert::ToString(atmosphereNitrogen) +
             ", CO2: " + Convert::ToString(atmosphereCarbonDioxide) +
             " kg in Atmosphere.");
    LOG_INFO("Planet Temperature: " + Convert::ToString(planetTemperature) +
             " Kelvin.");
}

void
    Planet::printVerbose()
{
    print();
    LOG_INFO("Atmospheric Filter.");
    printSpectrum(atmosphericFilter);
    LOG_INFO("Terrestrial Spectrum.");
    printSpectrum(terrestrialSpectrum);
}

void
    Planet::update()
{
    orbitalRadius++;
}

//! send the planet data to a Json object
Json::Value
    Planet::toJSON() const
{
    Json::Value result = CelestialBody::toJSON();

    result["oceanMass"] = oceanMass;
    result["lithosphereMass"] = lithosphereMass;
    result["atmosphereMass"] = atmosphereMass;
    result["atmosphereWater"] = atmosphereWater;
    result["atmosphereOxygen"] = atmosphereOxygen;
    result["atmosphereNitrogen"] = atmosphereNitrogen;
    result["atmosphereCarbonDioxide"] = atmosphereCarbonDioxide;
    result["planetTemperature"] = planetTemperature;
    result["habitability"] = habitability;
    result["orbitalRadiusGraphFraction"] = orbitalRadiusGraphFraction;

    Json::Value atmosphericFilterJ(Json::ValueType::arrayValue);
    for(auto spectrum : atmosphericFilter)
        atmosphericFilterJ.append(spectrum);
    result["atmosphericFilter"] = atmosphericFilterJ;

    Json::Value terrestrialSpectrumJ(Json::ValueType::arrayValue);
    for(auto spectrum : terrestrialSpectrum)
        terrestrialSpectrumJ.append(spectrum);
    result["terrestrialSpectrum"] = terrestrialSpectrumJ;

    return result;
}

//! convert the json object to a string.
std::string
    Planet::toJSONString() const
{
    std::stringstream sstream;
    const Json::Value value = toJSON();

    Json::StreamWriterBuilder builder;
    builder["indentation"] = " ";
    std::unique_ptr<Json::StreamWriter> writer(builder.newStreamWriter());

    writer->write(value, &sstream);
    // LOG_INFO(sstream.str());
    return sstream.str();
}
