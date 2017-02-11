#pragma once

#include <algorithm>
#include <memory>
#include <random>


namespace sol {
class state;
}

namespace thrive {

/**
* @brief Random Number Generator
*
* Uses C++11 RNG features for optimal quality RNG.
*/
class RNG final {

public:

    /**
    * @brief Typedef for seed values
    */
    using Seed = unsigned int; // parts of <random> uses unsigned int

    /**
    * @brief Lua bindings
    *
    * Exposes:
    *
    * - RNG::getInt()
    * - RNG::getDouble() (as <tt>getReal()</tt>)
    * - RNG::generateRandomSeed
    * - RNG::setSeed(seed)
    * - RNG::getSeed()
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Constructor using proper random seed
    */
    RNG();

    /**
    * @brief Constructor
    *
    * @param seed
    *   The seed used for initializing the RNG
    */
    RNG(Seed seed);

    /**
    * @brief Destructor
    */
    ~RNG();

    /**
    * @brief Restarts the RNG with provided seed
    *
    * @param seed
    *   The seed used for reinitializing the RNG
    */
    void
    setSeed(Seed seed);

    /**
    * @brief Returns the current seed
    *
    * @return
    *   The used seed
    */
    Seed
    getSeed() const;


    /**
    * @brief Generates a proper random seed
    *
    * @return
    *  Seed with high amount of entropy
    */
    Seed
    generateRandomSeed();

    /**
    * @brief Generates a random double between min and max
    *
    * @return
    *  Double in range [min, max]
    */
    double
    getDouble(
        double min, 
        double max
    );

    /**
    * @brief Generates a random integer between min and max
    *
    * @return
    *  int in range [min, max] inclusive
    */
    int
    getInt(
        int min, 
        int max
    );

    /**
    * @brief Shuffles iterable collection.
    *
    * @tparam iterType
    *   Iterator type
    *
    * @return
    *  int in range [min, max] inclusive
    */
    template<typename iterType>
    void 
    shuffle(
        iterType first,
        iterType last
    ) {
        std::shuffle(first, last, mersenneTwister());
    }


private:

    std::mt19937&
    mersenneTwister();

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

}
