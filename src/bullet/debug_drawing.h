#pragma once

#include "engine/system.h"

#include <LinearMath/btIDebugDraw.h>
#include <memory>

namespace Ogre {
class SceneManager;
}

namespace thrive {
 
class BulletDebugDrawer: public btIDebugDraw {
public:

    BulletDebugDrawer(
        Ogre::SceneManager* sceneManager
    );

    ~BulletDebugDrawer();

    void     
    drawLine(
        const btVector3& from, 
        const btVector3& to, 
        const btVector3& fromColour,
        const btVector3& toColour
    ) override;

    void     
    drawLine(
        const btVector3& from, 
        const btVector3& to, 
        const btVector3& colour
    ) override;

    void     
    drawTriangle(
        const btVector3& v0, 
        const btVector3& v1, 
        const btVector3& v2, 
        const btVector3& n0,
        const btVector3& n1,
        const btVector3& n2,
        const btVector3& colour, 
        btScalar
    ) override;

    void     
    drawTriangle(
        const btVector3& v0, 
        const btVector3& v1, 
        const btVector3& v2, 
        const btVector3& colour, 
        btScalar
    ) override;

    void     
    drawContactPoint(
        const btVector3& PointOnB, 
        const btVector3& normalOnB, 
        btScalar distance, 
        int lifeTime, 
        const btVector3& colour
    ) override;

    void     
    reportErrorWarning(
        const char *warningString
    ) override;

    void     
    draw3dText(
        const btVector3& location, 
        const char *textString
    ) override;

    void     
    setDebugMode(
        int debugMode
    ) override;

    int     
    getDebugMode() const override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};

/**
* @brief Renders physics debug information
*/
class BulletDebugDrawSystem : public System {
    
public:

    /**
    * @brief Constructor
    */
    BulletDebugDrawSystem();

    /**
    * @brief Destructor
    */
    ~BulletDebugDrawSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    */
    void init(Engine* engine) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
