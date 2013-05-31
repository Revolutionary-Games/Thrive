#include "bullet/debug_drawing.h"

#include "bullet/bullet_engine.h"
#include "bullet/bullet_ogre_conversion.h"
#include "ogre/ogre_engine.h"

#include <atomic>
#include <btBulletCollisionCommon.h>
#include <btBulletDynamicsCommon.h>
#include <OgreManualObject.h>
#include <OgreSceneManager.h>

#include <iostream>

using namespace thrive;

namespace {

struct ContactPoint {

    btVector3 point;

    btVector3 normal;

    btScalar distance;

    int lifeTime;

    btVector3 color;

};

}

////////////////////////////////////////////////////////////////////////////////
// BulletDebugSystem
////////////////////////////////////////////////////////////////////////////////

struct BulletDebugSystem::Implementation : public btIDebugDraw {

    void
    draw3dText(
        const btVector3& location,
        const char* textString
    ) override {
        (void) location;
        (void) textString;
    }

    void
    drawContactPoint(
        const btVector3& point,
        const btVector3& normal,
        btScalar distance,
        int lifeTime,
        const btVector3& color
    ) override {
        m_contactPoints.emplace_back(ContactPoint{
            point, 
            normal,
            distance,
            lifeTime,
            color
        });
    }

    void
    drawLine(
        const btVector3& from,
        const btVector3& to,
        const btVector3& color
    ) override {
        m_recorded.workingCopy().m_lines.emplace_back(Line{
            from,
            to,
            color
        });
        m_recorded.touch();
    }

    using btIDebugDraw::drawLine;

    void
    drawTriangle(
        const btVector3& v0,
        const btVector3& v1,
        const btVector3& v2,
        const btVector3& color,
        btScalar alpha
    ) override {
        (void) alpha;
        m_recorded.workingCopy().m_triangles.emplace_back(Triangle{
            v0, v1, v2,
            color
        });
        m_recorded.touch();
    }

    using btIDebugDraw::drawTriangle;

    int
    getDebugMode() const override {
        return m_debugMode;
    }

    void
    setDebugMode(
        int debugMode
    ) override {
        m_debugMode = DebugDrawModes(debugMode);
    }

    void
    reportErrorWarning(
        const char* warningString
    ) override {
        (void) warningString;
    }

    void
    updateContactPoints(
        int milliseconds
    ) {
        auto iter = m_contactPoints.begin();
        while(iter != m_contactPoints.end()) {
            iter->lifeTime -= milliseconds;
            if (iter->lifeTime <= 0) {
                iter = m_contactPoints.erase(iter);
            }
            else {
                ++iter;
            }
        }
    }

    std::list<ContactPoint> m_contactPoints;

    std::atomic<DebugDrawModes> m_debugMode;

    PhysicsOutputData<DebugFrame> m_recorded;

    btCollisionWorld* m_world = nullptr;

};


BulletDebugSystem::BulletDebugSystem()
  : m_impl(new Implementation())
{
}


BulletDebugSystem::~BulletDebugSystem() {}


void
BulletDebugSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_world == nullptr && "Double init of system");
    BulletEngine* bulletEngine = dynamic_cast<BulletEngine*>(engine);
    assert(bulletEngine != nullptr && "System requires a BulletEngine");
    m_impl->m_world = bulletEngine->world();
    m_impl->m_world->setDebugDrawer(m_impl.get());
}


void
BulletDebugSystem::setDebugMode(
    int mode
) {
    m_impl->setDebugMode(mode);
}


void
BulletDebugSystem::shutdown() {
    m_impl->m_world->setDebugDrawer(nullptr);
    System::shutdown();
}


void
BulletDebugSystem::update(
    int milliseconds
) {
    m_impl->updateContactPoints(milliseconds);
    m_impl->m_recorded.workingCopy().m_lines.clear();
    m_impl->m_recorded.workingCopy().m_triangles.clear();
    for (const auto& contactPoint : m_impl->m_contactPoints) {
        m_impl->drawLine(
            contactPoint.point,
            contactPoint.point + contactPoint.normal * contactPoint.distance,
            contactPoint.color
        );
    }
    m_impl->m_recorded.touch();
    m_impl->m_world->debugDrawWorld();
}


void
BulletDebugSystem::transfer() {
    if (m_impl->m_recorded.hasChanges()) {
        const auto& recorded = m_impl->m_recorded.stable();
        auto& debugFrame = m_debugFrame.workingCopy();
        debugFrame.m_lines.assign(recorded.m_lines.begin(), recorded.m_lines.end());
        debugFrame.m_triangles.assign(recorded.m_triangles.begin(), recorded.m_triangles.end());
        m_debugFrame.touch(); 
        m_impl->m_recorded.untouch();
    }
}


////////////////////////////////////////////////////////////////////////////////
// BulletDebugScriptSystem
////////////////////////////////////////////////////////////////////////////////

struct BulletDebugScriptSystem::Implementation {

    Implementation(
        std::shared_ptr<BulletDebugSystem> debugSystem
    ) : m_debugSystem(debugSystem)
    {
    }

    std::shared_ptr<BulletDebugSystem> m_debugSystem;

};


BulletDebugScriptSystem::BulletDebugScriptSystem(
    std::shared_ptr<BulletDebugSystem> debugSystem
) : m_impl(new Implementation(debugSystem))
{
}


BulletDebugScriptSystem::~BulletDebugScriptSystem() {}


void
BulletDebugScriptSystem::init(
    Engine*
) {
    // Nothing
}


void
BulletDebugScriptSystem::shutdown() {
    // Nothing
}


void
BulletDebugScriptSystem::update(int) {
    m_impl->m_debugSystem->transfer();
}



////////////////////////////////////////////////////////////////////////////////
// BulletDebugRenderSystem
////////////////////////////////////////////////////////////////////////////////

static const char MATERIAL_NAME[] = "BulletDebugDefault";

struct BulletDebugRenderSystem::Implementation {

    Implementation(
        std::shared_ptr<BulletDebugSystem> debugSystem
    ) : m_debugSystem(debugSystem)
    {
        this->setupMaterial();
    }

    void
    setupLines() {
        auto root = m_sceneManager->getRootSceneNode();
        m_lines.reset(new Ogre::ManualObject("Physics Debug Lines"));
        root->attachObject(m_lines.get());
        m_lines->begin(MATERIAL_NAME, Ogre::RenderOperation::OT_LINE_LIST);
        // Initialize to empty line
        for(int i=0; i < 2; ++i) {
            m_lines->position(Ogre::Vector3::ZERO);
            m_lines->colour(Ogre::ColourValue::Blue);
        }
        m_lines->end();
    }

    void
    setupMaterial() {
        auto& materialManager = Ogre::MaterialManager::getSingleton();
        Ogre::MaterialPtr material = materialManager.getDefaultSettings()->clone(MATERIAL_NAME);
        material->setReceiveShadows(false);
        material->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
        material->setDepthBias(0.1, 0);
        Ogre::TextureUnitState* textureUnit = material->getTechnique(0)->getPass(0)->createTextureUnitState();
        textureUnit->setColourOperationEx(Ogre::LBX_SOURCE1, Ogre::LBS_DIFFUSE);
        material->getTechnique(0)->setLightingEnabled(false);
    }

    void
    setupTriangles() {
        auto root = m_sceneManager->getRootSceneNode();
        m_triangles.reset(new Ogre::ManualObject("Physics Debug Triangles"));
        root->attachObject(m_triangles.get());
        m_triangles->begin(MATERIAL_NAME, Ogre::RenderOperation::OT_TRIANGLE_LIST);
        // Initialize to empty triangle
        for(int i=0; i < 3; ++i) {
            m_triangles->position(Ogre::Vector3::ZERO);
            m_triangles->colour(Ogre::ColourValue::Blue);
        }
        m_triangles->end();
    }

    std::shared_ptr<BulletDebugSystem> m_debugSystem;

    std::unique_ptr<Ogre::ManualObject> m_lines;

    Ogre::SceneManager* m_sceneManager = nullptr;

    std::unique_ptr<Ogre::ManualObject> m_triangles;

};



BulletDebugRenderSystem::BulletDebugRenderSystem(
    std::shared_ptr<BulletDebugSystem> debugSystem
) : m_impl(new Implementation(debugSystem))
{
}


BulletDebugRenderSystem::~BulletDebugRenderSystem() {}


void
BulletDebugRenderSystem::init(
    Engine* engine
) {
    System::init(engine);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    OgreEngine* ogreEngine = dynamic_cast<OgreEngine*>(engine);
    assert(ogreEngine != nullptr && "System requires an OgreEngine");
    m_impl->m_sceneManager = ogreEngine->sceneManager();
    m_impl->setupLines();
    m_impl->setupTriangles();
}


void
BulletDebugRenderSystem::shutdown() {
    m_impl->m_lines.reset();
    m_impl->m_triangles.reset();
    m_impl->m_sceneManager = nullptr;
}


void
BulletDebugRenderSystem::update(int) {
    const auto& frame = m_impl->m_debugSystem->m_debugFrame.stable();
    // Lines
    m_impl->m_lines->beginUpdate(0);
    for(const auto& line : frame.m_lines) {
        Ogre::ColourValue colour(
            line.m_color.getX(),
            line.m_color.getY(),
            line.m_color.getZ()
        );
        colour.saturate();
        m_impl->m_lines->position(bulletToOgre(line.m_from));
        m_impl->m_lines->colour(colour);
        m_impl->m_lines->position(bulletToOgre(line.m_to));
        m_impl->m_lines->colour(colour);
    }
    m_impl->m_lines->end();
    // Triangles
    m_impl->m_triangles->beginUpdate(0);
    for (const auto& triangle : frame.m_triangles) {
        Ogre::ColourValue colour(
            triangle.m_color.getX(),
            triangle.m_color.getY(),
            triangle.m_color.getZ()
        );
        colour.saturate();
        for (int i=0; i < 3; ++i) {
            m_impl->m_triangles->position(
                bulletToOgre(triangle.m_vertices[i])
            );
            m_impl->m_triangles->colour(colour);
        }

    }
    m_impl->m_triangles->end();
}




