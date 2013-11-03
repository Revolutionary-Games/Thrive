#pragma once

#include "engine/system.h"

#include <LinearMath/btIDebugDraw.h>
#include <memory>

namespace Ogre {
class SceneManager;
}

namespace thrive {
 
/**
* @brief Implementation of Bullet's debug drawing interface
*/
class BulletDebugDrawer: public btIDebugDraw {
public:

    /**
    * @brief Constructor
    *
    * @param sceneManager
    *   The scene manager to use for the graphics elements
    */
    BulletDebugDrawer(
        Ogre::SceneManager* sceneManager
    );

    /**
    * @brief Destructor
    */
    ~BulletDebugDrawer();

    /**
    * @brief Overridden from btIDebugDraw::drawLine
    *
    * @param from
    * @param to
    * @param fromColour
    * @param toColour
    */
    void     
    drawLine(
        const btVector3& from, 
        const btVector3& to, 
        const btVector3& fromColour,
        const btVector3& toColour
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::drawLine
    *
    * @param from
    * @param to
    * @param colour
    */
    void     
    drawLine(
        const btVector3& from, 
        const btVector3& to, 
        const btVector3& colour
    ) override;


    /**
    * @brief Overridden from btIDebugDraw::drawTriangle
    *
    * @param v0
    *   Vertex
    * @param v1
    * @param v2
    * @param n0
    *   Normal
    * @param n1
    * @param n2
    * @param colour
    * @param alpha
    */
    void     
    drawTriangle(
        const btVector3& v0, 
        const btVector3& v1, 
        const btVector3& v2, 
        const btVector3& n0,
        const btVector3& n1,
        const btVector3& n2,
        const btVector3& colour, 
        btScalar alpha
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::drawTriangle
    *
    * @param v0
    * @param v1
    * @param v2
    * @param colour
    * @param alpha
    */
    void     
    drawTriangle(
        const btVector3& v0, 
        const btVector3& v1, 
        const btVector3& v2, 
        const btVector3& colour, 
        btScalar alpha
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::drawTriangle
    *
    * @param PointOnB
    * @param normalOnB
    * @param distance
    * @param lifeTime
    * @param colour
    */
    void     
    drawContactPoint(
        const btVector3& PointOnB, 
        const btVector3& normalOnB, 
        btScalar distance, 
        int lifeTime, 
        const btVector3& colour
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::reportErrorWarning
    *
    * @param warningString
    */
    void     
    reportErrorWarning(
        const char *warningString
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::draw3dText
    *
    * @param location
    * @param textString
    */
    void     
    draw3dText(
        const btVector3& location, 
        const char *textString
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::setDebugMode
    *
    * @param debugMode
    */
    void     
    setDebugMode(
        int debugMode
    ) override;

    /**
    * @brief Overridden from btIDebugDraw::getDebugMode
    *
    */
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
    * @brief Lua bindings
    *
    * Exposes:
    * - BulletDebugDrawSystem()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

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
    */
    void init(GameState* gameState) override;

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
