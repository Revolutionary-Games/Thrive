#pragma once

#include "engine/shared_data.h"
#include "engine/system.h"

#include <array>
#include <LinearMath/btVector3.h>
#include <list>
#include <memory>

namespace thrive {

/**
* @brief Records debug draw calls made by the physics engine
*/
class BulletDebugSystem : public System {

public:

    /**
    * @brief A line that is supposed to be drawn
    */
    struct Line {

        /**
        * @brief Constructor
        *
        * @param from
        * @param to
        * @param color
        */
        Line(
            const btVector3& from,
            const btVector3& to,
            const btVector3& color
        ) : m_from(from),
            m_to(to),
            m_color(color)
        {
        }

        /**
        * @brief Start point
        */
        btVector3 m_from = {0, 0, 0};

        /**
        * @brief End point
        */
        btVector3 m_to = {0, 0, 0};

        /**
        * @brief Color (duh)
        */
        btVector3 m_color = {0, 0, 0};

    };


    /**
    * @brief A triangle that is supposed to be drawn
    */
    struct Triangle {

        /**
        * @brief Constructor
        *
        * @param v0
        * @param v1
        * @param v2
        * @param color
        */
        Triangle(
            const btVector3& v0,
            const btVector3& v1,
            const btVector3& v2,
            const btVector3& color
        ) : m_vertices{{v0, v1, v2}},
            m_color(color)
        {
        }

        /**
        * @brief Vertices
        */
        std::array<btVector3, 3> m_vertices = {{
            {0, 0, 0},
            {0, 0, 0},
            {0, 0, 0}
        }};

        /**
        * @brief Color
        */
        btVector3 m_color = {0, 0, 0};

    };

    /**
    * @brief A frame's worth of debug render information
    */
    struct DebugFrame {

        /**
        * @brief Lines to be rendered
        */
        std::list<Line> m_lines = {};

        /**
        * @brief Triangles to be rendered
        */
        std::list<Triangle> m_triangles = {};

    };

    /**
    * @brief Constructor
    */
    BulletDebugSystem();

    /**
    * @brief Destructor
    */
    ~BulletDebugSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Must be a BulletEngine
    */
    void
    init(
        Engine* engine
    ) override;

    /**
    * @brief Sets the debug primitives that will be drawn
    *
    * @param mode
    *   The new debug mode, a bitset of flags. See 
    *   btIDebugDraw::DebugDrawModes for details.
    */
    void
    setDebugMode(
        int mode
    );

    /**
    * @brief Shuts the system down
    */
    void
    shutdown() override;

    /**
    * @brief Records a debug frame
    *
    * @param milliseconds
    */
    void
    update(
        int milliseconds
    ) override;

    /**
    * @brief Transfers a debug frame to the Ogre thread
    *
    * @warning
    *   Only call this from the script thread
    */
    void
    transfer();

    /**
    * @brief A frame of debug information, ready to be rendered
    */
    RenderData<DebugFrame> 
    m_debugFrame;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


/**
* @brief Transfers debug information from the physics thread to the render thread
*/
class BulletDebugScriptSystem : public System {

public:

    /**
    * @brief Constructor
    */
    BulletDebugScriptSystem(
        std::shared_ptr<BulletDebugSystem> debugSystem
    );

    /**
    * @brief Destructor
    */
    ~BulletDebugScriptSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Should be the ScriptEngine
    */
    void
    init(
        Engine* engine
    ) override;

    /**
    * @brief Shuts the system down
    */
    void
    shutdown() override;

    /**
    * @brief Updates the system
    *
    * @param milliseconds
    */
    void
    update(
        int milliseconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


/**
* @brief Renders physics debug information onto the screen
*/
class BulletDebugRenderSystem : public System {

public:

    /**
    * @brief Constructor
    */
    BulletDebugRenderSystem(
        std::shared_ptr<BulletDebugSystem> debugSystem
    );

    /**
    * @brief Destructor
    */
    ~BulletDebugRenderSystem();

    /**
    * @brief Initializes the system
    *
    * @param engine
    *   Must be an OgreEngine
    */
    void
    init(
        Engine* engine
    ) override;

    /**
    * @brief Shuts the system down
    */
    void
    shutdown() override;

    /**
    * @brief Updates the system
    *
    * @param milliseconds
    */
    void
    update(
        int milliseconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};



}
