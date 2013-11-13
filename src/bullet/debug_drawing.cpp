#include "bullet/debug_drawing.h"

#include "bullet/bullet_ogre_conversion.h"
#include "engine/game_state.h"
#include "scripting/luabind.h"

#include <OgreColourValue.h>
#include <OgreFrameListener.h>
#include <OgreManualObject.h>
#include <OgreRoot.h>
#include <OgreVector3.h>

using namespace thrive;

static const char MATERIAL_NAME[] = "OgreBulletCollisionsDebugDefault";

static Ogre::ColourValue
toOgreColour(
    const btVector3& colour,
    btScalar alpha = 1.0
) {
    Ogre::ColourValue ogreColour(
        colour.getX(),
        colour.getY(),
        colour.getZ(),
        alpha
    );
    ogreColour.saturate();
    return ogreColour;
}

struct BulletDebugDrawer::Implementation : public Ogre::FrameListener {

    struct ContactPoint{
        Ogre::Vector3 from;
        Ogre::Vector3 to;
        Ogre::ColourValue colour;
        size_t dieTime;
    };

    Implementation()
      : m_lines(new Ogre::ManualObject("physics lines")),
        m_triangles(new Ogre::ManualObject("physics triangles"))
    {
        m_lines->setDynamic(true);
        m_triangles->setDynamic(true);
        this->setupMaterial();
        this->setupLines();
        this->setupTriangles();
        Ogre::Root::getSingleton().addFrameListener(this);
    }

    ~Implementation() {
        Ogre::Root::getSingleton().removeFrameListener(this);
    }

    bool
    frameStarted(
        const Ogre::FrameEvent&
    ) override {
        size_t now = Ogre::Root::getSingleton().getTimer()->getMilliseconds();
        auto iter = m_contactPoints.begin();
        while (iter != m_contactPoints.end()) {
            const ContactPoint& contactPoint = *iter;
            m_lines->position(contactPoint.from);
            m_lines->colour(contactPoint.colour);
            m_lines->position(contactPoint.to);
            m_lines->colour(contactPoint.colour);
            if (contactPoint.dieTime <= now) {
                iter = m_contactPoints.erase(iter);
            }
            else {
                ++iter;
            }
        }
        m_lines->end();
        m_triangles->end();
        return true;
    }

    bool
    frameEnded(
        const Ogre::FrameEvent&
    ) override {
        m_lines->beginUpdate(0);
        m_triangles->beginUpdate(0);
        return true;
    }

    void
    setupLines() {
        m_lines->begin(
            MATERIAL_NAME,
            Ogre::RenderOperation::OT_LINE_LIST
        );
        for (int i = 0; i < 2; ++i) {
            m_lines->position(Ogre::Vector3::ZERO);
            m_lines->colour(Ogre::ColourValue::Blue);
        }
    }

    void
    setupMaterial() {
        Ogre::MaterialPtr material = Ogre::MaterialManager::getSingleton().getDefaultSettings()->clone(
            MATERIAL_NAME
        );
        material->setReceiveShadows(false);
        material->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
        material->setDepthBias(0.1, 0);
        Ogre::TextureUnitState* textureUnitState = material->getTechnique(0)->getPass(0)->createTextureUnitState();
        textureUnitState->setColourOperationEx(
            Ogre::LBX_SOURCE1,
            Ogre::LBS_DIFFUSE
        );
        material->getTechnique(0)->setLightingEnabled(false);
    }

    void
    setupTriangles() {
        m_triangles->begin(
            MATERIAL_NAME,
            Ogre::RenderOperation::OT_TRIANGLE_LIST
        );
        for (int i=0; i < 3; ++i) {
            m_triangles->position(Ogre::Vector3::ZERO);
            m_triangles->colour(Ogre::ColourValue::Blue);
        }
    }

    DebugDrawModes m_debugModes = DBG_DrawWireframe;

    std::unique_ptr<Ogre::ManualObject> m_lines;

    std::unique_ptr<Ogre::ManualObject> m_triangles;

    std::list<ContactPoint> m_contactPoints;
};


BulletDebugDrawer::BulletDebugDrawer(
    Ogre::SceneManager* sceneManager
) : m_impl(new Implementation())
{
    sceneManager->getRootSceneNode()->attachObject(
        m_impl->m_lines.get()
    );
    sceneManager->getRootSceneNode()->attachObject(
        m_impl->m_triangles.get()
    );
}


BulletDebugDrawer::~BulletDebugDrawer() { }


void
BulletDebugDrawer::drawLine(
    const btVector3& from,
    const btVector3& to,
    const btVector3& fromColour,
    const btVector3& toColour
) {
    m_impl->m_lines->position(bulletToOgre(from));
    m_impl->m_lines->colour(toOgreColour(fromColour));
    m_impl->m_lines->position(bulletToOgre(to));
    m_impl->m_lines->colour(toOgreColour(toColour));
}


void
BulletDebugDrawer::drawLine(
    const btVector3& from,
    const btVector3& to,
    const btVector3& colour
) {
    this->drawLine(from, to, colour, colour);
}


void
BulletDebugDrawer::drawTriangle(
    const btVector3& v0,
    const btVector3& v1,
    const btVector3& v2,
    const btVector3& colour,
    btScalar alpha
) {
    Ogre::ColourValue ogreColour = toOgreColour(colour, alpha);
    for (const btVector3& vertex : {v0, v1, v2}) {
        m_impl->m_triangles->position(bulletToOgre(vertex));
        m_impl->m_triangles->colour(ogreColour);
    }
}


void
BulletDebugDrawer::drawTriangle(
    const btVector3& v0,
    const btVector3& v1,
    const btVector3& v2,
    const btVector3& n0,
    const btVector3& n1,
    const btVector3& n2,
    const btVector3& colour,
    btScalar alpha
) {
    (void) n0;
    (void) n1;
    (void) n2;
    this->drawTriangle(v0, v1, v2, colour, alpha);
}

void
BulletDebugDrawer::drawContactPoint(
    const btVector3& point,
    const btVector3& normal,
    btScalar distance,
    int lifeTime,
    const btVector3& colour
) {
    size_t dieTime = Ogre::Root::getSingleton().getTimer()->getMilliseconds() + lifeTime;
    m_impl->m_contactPoints.push_back(Implementation::ContactPoint{
        bulletToOgre(point),
        bulletToOgre(point + distance * normal),
        toOgreColour(colour),
        dieTime
    });
}


void
BulletDebugDrawer::reportErrorWarning(
    const char *warningString
) {
    Ogre::LogManager::getSingleton().getDefaultLog()->logMessage(
        warningString
    );
}

void
BulletDebugDrawer::draw3dText(
    const btVector3& location,
    const char* textString
) {
    (void) location;
    (void) textString;
}

void
BulletDebugDrawer::setDebugMode(
    int debugMode
) {
    m_impl->m_debugModes = DebugDrawModes(debugMode);
}

int
BulletDebugDrawer::getDebugMode() const
{
    return m_impl->m_debugModes;
}



////////////////////////////////////////////////////////////////////////////////
// BulletDebugDrawSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
BulletDebugDrawSystem::luaBindings() {
    using namespace luabind;
    return class_<BulletDebugDrawSystem, System>("BulletDebugDrawSystem")
        .def(constructor<>())
    ;
}


struct BulletDebugDrawSystem::Implementation {

    std::unique_ptr<BulletDebugDrawer> m_debugDrawer;

    btDynamicsWorld* m_physicsWorld = nullptr;

};


BulletDebugDrawSystem::BulletDebugDrawSystem()
  : m_impl(new Implementation())
{
}


BulletDebugDrawSystem::~BulletDebugDrawSystem() {}


void
BulletDebugDrawSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    assert(m_impl->m_physicsWorld == nullptr && "Double init of system");
    m_impl->m_debugDrawer.reset(new BulletDebugDrawer(
            gameState->sceneManager()
    ));
    m_impl->m_physicsWorld = gameState->physicsWorld();
    m_impl->m_physicsWorld->setDebugDrawer(
        m_impl->m_debugDrawer.get()
    );
}


void
BulletDebugDrawSystem::shutdown() {
    m_impl->m_physicsWorld->setDebugDrawer(nullptr);
    m_impl->m_debugDrawer.reset();
    m_impl->m_physicsWorld = nullptr;
    System::shutdown();
}


void
BulletDebugDrawSystem::update(int) {
    m_impl->m_physicsWorld->debugDrawWorld();
}

