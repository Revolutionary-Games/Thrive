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

    struct Line {

        Line(
            const btVector3& from,
            const btVector3& to,
            const btVector3& color
        ) : m_from(from),
            m_to(to),
            m_color(color)
        {
        }

        btVector3 m_from = {0, 0, 0};

        btVector3 m_to = {0, 0, 0};

        btVector3 m_color = {0, 0, 0};

    };


    struct Triangle {

        Triangle(
            const btVector3& v0,
            const btVector3& v1,
            const btVector3& v2,
            const btVector3& color
        ) : m_vertices{{v0, v1, v2}},
            m_color(color)
        {
        }

        std::array<btVector3, 3> m_vertices = {{
            {0, 0, 0},
            {0, 0, 0},
            {0, 0, 0}
        }};

        btVector3 m_color = {0, 0, 0};

    };

    struct DebugFrame {

        std::list<Line> m_lines = {};

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

    void
    init(
        Engine* engine
    ) override;

    void
    setDebugMode(
        int mode
    );

    void
    shutdown() override;

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

    RenderData<DebugFrame> 
    m_debugFrame;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


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

    void
    init(
        Engine* engine
    ) override;

    void
    shutdown() override;

    void
    update(
        int milliseconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};


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

    void
    init(
        Engine* engine
    ) override;

    void
    shutdown() override;

    void
    update(
        int milliseconds
    ) override;

private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;

};



}
