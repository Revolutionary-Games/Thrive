#pragma once

#include "engine/system.h"

namespace thrive {

/**
* @brief System for saving the game
*/
class SaveSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    SaveSystem();

    /**
    * @brief Destructor
    */
    ~SaveSystem();

    /**
    * @brief Saves the game at the end of this frame
    *
    * @param filename
    *   The filename to save to
    */
    void
    save(
        std::string filename
    );

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

/**
* @brief System for loading a game
*/
class LoadSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    LoadSystem();

    /**
    * @brief Destructor
    */
    ~LoadSystem();

    /**
    * @brief Loads a savegame at the beginning of next frame
    *
    * @param filename
    *   The file to load from
    */
    void
    load(
        std::string filename
    );

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
