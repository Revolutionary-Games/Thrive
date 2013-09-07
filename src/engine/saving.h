#pragma once

#include "engine/system.h"

namespace thrive {

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
