#include "ogre/scene_node_system.h"

#include "engine/component_factory.h"
#include "engine/entity.h"
#include "engine/entity_filter.h"
#include "engine/entity_manager.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "scripting/luabind.h"

#include "sound/sound_source_system.h"
#include "sound/sound_manager.h"
#include "sound/sound_listener.h"

#include <OgreSceneManager.h>
#include <OgreMeshManager.h>
#include <OgreEntity.h>


#include <game.h>
#include <engine/engine.h>
#include <engine/rng.h>
#include <OgreRoot.h>
#include <OgreSubMesh.h>
#include <OgreMaterialManager.h>
#include <OgreTechnique.h>

#include <string>
#include <iostream>
#include <fstream>

using namespace thrive;


static Ogre::String
OgreSceneNodeComponent_getMeshName(
    const OgreSceneNodeComponent* self
) {
    return self->m_meshName.get();
}


static void
OgreSceneNodeComponent_setMeshName(
    OgreSceneNodeComponent* self,
    const Ogre::String& meshName
) {
    self->m_meshName = meshName;
}


static bool
OgreSceneNodeComponent_getVisible(
    const OgreSceneNodeComponent* self
) {
    return self->m_visible.get();
}


static void
OgreSceneNodeComponent_setVisible(
    OgreSceneNodeComponent* self,
    bool visible
) {
    self->m_visible = visible; // This should automatically call touch().w
}

static std::string
OgreSceneNodeComponent_getPlaneTexture(
    const OgreSceneNodeComponent* self
) {
    return self->m_planeTexture.get();
}


static void
OgreSceneNodeComponent_setPlaneTexture(
    OgreSceneNodeComponent* self,
    std::string planeTexture
) {
    self->m_planeTexture = planeTexture; // This should automatically call touch().w
}



static Entity
OgreSceneNodeComponent_getParent(
    const OgreSceneNodeComponent* self
) {
    return Entity(self->m_parentId.get());
}


static void
OgreSceneNodeComponent_setParent(
    OgreSceneNodeComponent* self,
    const Entity& entity
) {
    self->m_parentId = entity.id();
    self->m_parentId.touch();
}


luabind::scope
OgreSceneNodeComponent::luaBindings() {
    using namespace luabind;
    return class_<OgreSceneNodeComponent, Component>("OgreSceneNodeComponent")
        .enum_("ID") [
            value("TYPE_ID", OgreSceneNodeComponent::TYPE_ID)
        ]
        .scope [
            def("TYPE_NAME", &OgreSceneNodeComponent::TYPE_NAME),
            class_<Transform, Touchable>("Transform")
                .def_readwrite("orientation", &Transform::orientation)
                .def_readwrite("position", &Transform::position)
                .def_readwrite("scale", &Transform::scale)
        ]
        .def(constructor<>())
        .def("playAnimation", &OgreSceneNodeComponent::playAnimation)
        .def("stopAnimation", &OgreSceneNodeComponent::stopAnimation)
        .def("stopAllAnimations", &OgreSceneNodeComponent::stopAllAnimations)
        .def("setAnimationSpeed", &OgreSceneNodeComponent::setAnimationSpeed)
        .def("attachObject", &OgreSceneNodeComponent::attachObject)
        .def("attachSoundListener", &OgreSceneNodeComponent::attachSoundListener)
        .def("sendOrganelles", &OgreSceneNodeComponent::sendOrganelles)
        .def_readonly("transform", &OgreSceneNodeComponent::m_transform)
        .def_readonly("entity", &OgreSceneNodeComponent::m_entity)
        .property("parent", OgreSceneNodeComponent_getParent, OgreSceneNodeComponent_setParent)
        .property("meshName", OgreSceneNodeComponent_getMeshName, OgreSceneNodeComponent_setMeshName)
        .property("visible", OgreSceneNodeComponent_getVisible, OgreSceneNodeComponent_setVisible)
        .property("planeTexture", OgreSceneNodeComponent_getPlaneTexture, OgreSceneNodeComponent_setPlaneTexture)
    ;
}

bool OgreSceneNodeComponent::s_soundListenerAttached = false;

void
OgreSceneNodeComponent::load(
    const StorageContainer& storage
) {
    Component::load(storage);
    m_transform.orientation = storage.get<Ogre::Quaternion>("orientation", Ogre::Quaternion::IDENTITY);
    m_transform.position = storage.get<Ogre::Vector3>("position", Ogre::Vector3(0,0,0));
    m_transform.scale = storage.get<Ogre::Vector3>("scale", Ogre::Vector3(1,1,1));
    m_meshName = storage.get<Ogre::String>("meshName");
    m_meshName = storage.get<Ogre::String>("meshName");
    m_visible = storage.get<bool>("visible");
    m_planeTexture = storage.get<Ogre::String>("planeTexture");
    m_parentId = storage.get<EntityId>("parentId", NULL_ENTITY);
}


StorageContainer
OgreSceneNodeComponent::storage() const {
    StorageContainer storage = Component::storage();
    storage.set<Ogre::Quaternion>("orientation", m_transform.orientation);
    storage.set<Ogre::Vector3>("position", m_transform.position);
    storage.set<Ogre::Vector3>("scale", m_transform.scale);
    storage.set<Ogre::String>("meshName", m_meshName);
    storage.set<bool>("visible", m_visible);
    storage.set<Ogre::String>("planeTexture", m_planeTexture);
    storage.set<EntityId>("parentId", m_parentId);
    return storage;
}

void
OgreSceneNodeComponent::playAnimation(
    std::string name,
    bool loop
) {
    m_animationsToStart.push_back(std::pair<std::string, bool>(name, loop));
    m_animationChange = true;
}

void
OgreSceneNodeComponent::setAnimationSpeed(
    float factor
) {
    m_animationSpeedFactor = factor;
}

void
OgreSceneNodeComponent::stopAnimation(
    std::string name
) {
    m_animationsToHalt.push_back(name);
    m_animationChange = true;
}

void
OgreSceneNodeComponent::stopAllAnimations() {
    m_fullAnimationHalt = true;
    m_animationChange = true;
}

void
OgreSceneNodeComponent::attachObject(
    Ogre::MovableObject* obj
) {
    m_objectsToAttach.get().push_back(obj);
    m_objectsToAttach.touch();
}

void
OgreSceneNodeComponent::_attachObject(
    Ogre::MovableObject* obj
) {
    m_sceneNode->attachObject(obj);
}

void
OgreSceneNodeComponent::attachSoundListener() {
    m_attachToListener = true;
    m_attachToListener.touch();
}

REGISTER_COMPONENT(OgreSceneNodeComponent)

////////////////////////////////////////////////////////////////////////////////
// OgreAddSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreAddSceneNodeSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreAddSceneNodeSystem, System>("OgreAddSceneNodeSystem")
        .def(constructor<>())
    ;
}


struct OgreAddSceneNodeSystem::Implementation {

    Ogre::SceneManager* m_sceneManager = nullptr;

    EntityFilter<OgreSceneNodeComponent> m_entities = {true};
};


OgreAddSceneNodeSystem::OgreAddSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreAddSceneNodeSystem::~OgreAddSceneNodeSystem() {}


void
OgreAddSceneNodeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreAddSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreAddSceneNodeSystem::update(int, int) {
    auto& added = m_impl->m_entities.addedEntities();
    for (const auto& entry : added) {
        OgreSceneNodeComponent* component = std::get<0>(entry.second);
        Ogre::SceneNode* parentNode = nullptr;
        EntityId parentId = component->m_parentId;
        if (parentId == NULL_ENTITY) {
            parentNode = m_impl->m_sceneManager->getRootSceneNode();
            component->m_parentId.untouch();
        }
        else {
            auto parentComponent = this->entityManager()->getComponent<OgreSceneNodeComponent>(parentId);
            if (parentComponent and parentComponent->m_sceneNode) {
                parentNode = parentComponent->m_sceneNode;
                component->m_parentId.untouch();
            }
            else {
                parentNode = m_impl->m_sceneManager->getRootSceneNode();
                // Mark component for later reparenting
                component->m_parentId.touch();
            }
        }
        Ogre::SceneNode* node = parentNode->createChildSceneNode();
        component->m_sceneNode = node;
    }
    m_impl->m_entities.clearChanges();
}


////////////////////////////////////////////////////////////////////////////////
// OgreRemoveSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreRemoveSceneNodeSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreRemoveSceneNodeSystem, System>("OgreRemoveSceneNodeSystem")
        .def(constructor<>())
    ;
}


struct OgreRemoveSceneNodeSystem::Implementation {

    std::unordered_map<EntityId, Ogre::Entity*> m_ogreEntities;

    Ogre::SceneManager* m_sceneManager = nullptr;

    std::unordered_map<EntityId, Ogre::SceneNode*> m_sceneNodes;

    EntityFilter<OgreSceneNodeComponent> m_entities = {true};
};


OgreRemoveSceneNodeSystem::OgreRemoveSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreRemoveSceneNodeSystem::~OgreRemoveSceneNodeSystem() {}


void
OgreRemoveSceneNodeSystem::init(
    GameState* gameState
) {
    Ogre::Animation::setDefaultInterpolationMode(Ogre::Animation::IM_LINEAR);
    Ogre::Animation::setDefaultRotationInterpolationMode(Ogre::Animation::RIM_LINEAR);
    System::init(gameState);
    assert(m_impl->m_sceneManager == nullptr && "Double init of system");
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreRemoveSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}


void
OgreRemoveSceneNodeSystem::update(int, int) {
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        // Scene node
        Ogre::SceneNode* node = m_impl->m_sceneNodes[entityId];
        if (node) {
            node->detachAllObjects();
            m_impl->m_sceneManager->destroySceneNode(node);
        }
        m_impl->m_sceneNodes.erase(entityId);
        // Ogre Entity
        Ogre::Entity* entity = m_impl->m_ogreEntities[entityId];
        if (entity) {
            m_impl->m_sceneManager->destroyEntity(entity);
        }
        m_impl->m_ogreEntities.erase(entityId);
    }
    for (auto& value : m_impl->m_entities.addedEntities()) {
        EntityId entityId = value.first;
        OgreSceneNodeComponent* sceneNodeComponent = std::get<0>(value.second);
        m_impl->m_ogreEntities[entityId] = sceneNodeComponent->m_entity;
        m_impl->m_sceneNodes[entityId] = sceneNodeComponent->m_sceneNode;
    }
    m_impl->m_entities.clearChanges();
}


////////////////////////////////////////////////////////////////////////////////
// OgreUpdateSceneNodeSystem
////////////////////////////////////////////////////////////////////////////////

luabind::scope
OgreUpdateSceneNodeSystem::luaBindings() {
    using namespace luabind;
    return class_<OgreUpdateSceneNodeSystem, System>("OgreUpdateSceneNodeSystem")
        .def(constructor<>())
    ;
}


struct OgreUpdateSceneNodeSystem::Implementation {

    EntityFilter<
        OgreSceneNodeComponent
    > m_entities;

    Ogre::SceneManager* m_sceneManager = nullptr;

};


OgreUpdateSceneNodeSystem::OgreUpdateSceneNodeSystem()
  : m_impl(new Implementation())
{
}


OgreUpdateSceneNodeSystem::~OgreUpdateSceneNodeSystem() {}


void
OgreUpdateSceneNodeSystem::init(
    GameState* gameState
) {
    System::init(gameState);
    m_impl->m_sceneManager = gameState->sceneManager();
    m_impl->m_entities.setEntityManager(&gameState->entityManager());
}


void
OgreUpdateSceneNodeSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    m_impl->m_sceneManager = nullptr;
    System::shutdown();
}

static int planeNameCounter = 0;

void
OgreUpdateSceneNodeSystem::update(
    int,
    int logicTime
) {
    for (const auto& entry : m_impl->m_entities) {
        OgreSceneNodeComponent* component = std::get<0>(entry.second);
        Ogre::SceneNode* sceneNode = component->m_sceneNode;
        auto& transform = component->m_transform;
        if (transform.hasChanges()) {
            sceneNode->setOrientation(
                transform.orientation
            );
            sceneNode->setPosition(
                transform.position
            );
            sceneNode->setScale(
                transform.scale
            );
            transform.untouch();
        }
        if (component->m_parentId.hasChanges()) {
            EntityId parentId = component->m_parentId;
            Ogre::SceneNode* newParentNode = nullptr;
            if (parentId == NULL_ENTITY) {
                newParentNode = m_impl->m_sceneManager->getRootSceneNode();
                component->m_parentId.untouch();
            }
            else {
                auto parentComponent = this->entityManager()->getComponent<OgreSceneNodeComponent>(
                    parentId
                );
                if (parentComponent and parentComponent->m_sceneNode) {
                    newParentNode = parentComponent->m_sceneNode;
                    component->m_parentId.untouch();
                }
                else {
                    newParentNode = m_impl->m_sceneManager->getRootSceneNode();
                    // Mark component for later reparenting
                    component->m_parentId.touch();
                }
            }
            Ogre::SceneNode* currentParentNode = sceneNode->getParentSceneNode();
            currentParentNode->removeChild(sceneNode);
            newParentNode->addChild(sceneNode);
        }
        if (component->m_meshName.hasChanges()) {
            if (component->m_entity) {
                sceneNode->detachObject(component->m_entity);
                m_impl->m_sceneManager->destroyEntity(component->m_entity);
                component->m_entity = nullptr;
            }

            if (component->m_meshName.get().find("membrane") != std::string::npos)
            {
                //int value = luabind::call_function<int>(Game::instance().engine().luaState(), "SendOrganelles", false);

                //MessageBox(NULL, std::to_string(value).c_str(), "Test", 0);

                // Get the vertex positions of the membrane.
                if(!component->MyMembrane.isInitialized)
                {
                    component->MyMembrane.Initialize(component->organellePositions);
                }
                component->MyMembrane.Update(component->organellePositions);

//                std::ofstream outputFile("membrane.txt");
//
//                outputFile << "organellePositions:" << " ";
//                for(size_t i=0; i<component->organellePositions.size(); i++)
//                {
//                    outputFile << component->organellePositions[i] << " ";
//                }
//                outputFile << std::endl;
//
//                outputFile << "organellePos:" << " ";
//                for(size_t i=0; i<component->MyMembrane.organellePos.size(); i++)
//                {
//                    outputFile << component->MyMembrane.organellePos[i] << " ";
//                }
//
//                outputFile.close();


                // Create a mesh and a submesh.
                Ogre::MeshPtr msh = Ogre::MeshManager::getSingleton().createManual(component->m_meshName.get(), "General");
                Ogre::SubMesh* sub = msh->createSubMesh();

                // Define the vertices.
                std::vector<double> vertexData;
                for(size_t i=0, end=component->MyMembrane.MeshPoints.size(); i<end; i++)
                {
                    // Vertex.
                    vertexData.push_back(component->MyMembrane.MeshPoints[i].x);
                    vertexData.push_back(component->MyMembrane.MeshPoints[i].y);
                    vertexData.push_back(component->MyMembrane.MeshPoints[i].z);

                    // Normal.
                    vertexData.push_back(0.0);
                    vertexData.push_back(1.0);
                    vertexData.push_back(1.0);
                }

                // Populate the vertex buffer.
                const size_t vertexBufferSize = vertexData.size();
                float vertices[vertexBufferSize];
                for(size_t i=0; i<vertexBufferSize; i++)
                {
                    vertices[i] = vertexData[i];
                }

                // Use render system to convert color value.
                Ogre::RenderSystem* rs = Ogre::Root::getSingleton().getRenderSystem();
                Ogre::RGBA color[vertexData.size()/6];
                Ogre::RGBA *pColor = color;
                for(size_t i=0, end=vertexData.size()/6; i<end; i++)
                {
                    rs->convertColourValue(Ogre::ColourValue(0.0,1.0,1.0), pColor++);
                }

                // Populate the index buffer.
                const size_t indexBufferSize = vertexData.size()/2;
                unsigned short faces[indexBufferSize];
                for(size_t i=0, end=indexBufferSize; i<end; i++)
                {
                    faces[i]=i;
                }

                // Create vertex data structure for 8 vertices shared between submeshes.
                msh->sharedVertexData = new Ogre::VertexData();
                msh->sharedVertexData->vertexCount = vertexData.size()/6;

                /// Create declaration (memory format) of vertex data
                Ogre::VertexDeclaration* decl = msh->sharedVertexData->vertexDeclaration;
                size_t offset = 0;
                // 1st buffer
                decl->addElement(0, offset, Ogre::VET_FLOAT3, Ogre::VES_POSITION);
                offset += Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT3);
                decl->addElement(0, offset, Ogre::VET_FLOAT3, Ogre::VES_NORMAL);
                offset += Ogre::VertexElement::getTypeSize(Ogre::VET_FLOAT3);
                /// Allocate vertex buffer of the requested number of vertices (vertexCount)
                /// and bytes per vertex (offset)
                Ogre::HardwareVertexBufferSharedPtr vbuf =
                    Ogre::HardwareBufferManager::getSingleton().createVertexBuffer(
                    offset, msh->sharedVertexData->vertexCount, Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);
                /// Upload the vertex data to the card
                vbuf->writeData(0, vbuf->getSizeInBytes(), vertices, true);

                /// Set vertex buffer binding so buffer 0 is bound to our vertex buffer
                Ogre::VertexBufferBinding* bind = msh->sharedVertexData->vertexBufferBinding;
                bind->setBinding(0, vbuf);

                // 2nd buffer
                offset = 0;
                decl->addElement(1, offset, Ogre::VET_COLOUR, Ogre::VES_DIFFUSE);
                offset += Ogre::VertexElement::getTypeSize(Ogre::VET_COLOUR);
                /// Allocate vertex buffer of the requested number of vertices (vertexCount)
                /// and bytes per vertex (offset)
                vbuf = Ogre::HardwareBufferManager::getSingleton().createVertexBuffer(
                    offset, msh->sharedVertexData->vertexCount, Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);
                /// Upload the vertex data to the card
                vbuf->writeData(0, vbuf->getSizeInBytes(), color, true);

                /// Set vertex buffer binding so buffer 1 is bound to our colour buffer
                bind->setBinding(1, vbuf);

                /// Allocate index buffer of the requested number of vertices (ibufCount)
                Ogre::HardwareIndexBufferSharedPtr ibuf = Ogre::HardwareBufferManager::getSingleton().
                    createIndexBuffer(
                    Ogre::HardwareIndexBuffer::IT_16BIT,
                    indexBufferSize,
                    Ogre::HardwareBuffer::HBU_STATIC_WRITE_ONLY);

                /// Upload the index data to the card
                ibuf->writeData(0, ibuf->getSizeInBytes(), faces, true);

                /// Set parameters of the submesh
                sub->useSharedVertices = true;
                sub->indexData->indexBuffer = ibuf;
                sub->indexData->indexCount = indexBufferSize;
                sub->indexData->indexStart = 0;

                /// Set bounding information (for culling)
                msh->_setBounds(Ogre::AxisAlignedBox(-50,-50,-50,50,50,50));
                msh->_setBoundingSphereRadius(50);

                /// Notify -Mesh object that it has been loaded
                msh->load();

                Ogre::MaterialPtr material = Ogre::MaterialManager::getSingleton().create(
                      "Test/ColourTest", Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME);
                material->getTechnique(0)->getPass(0)->setVertexColourTracking(Ogre::TVC_AMBIENT);

                Ogre::Entity* thisEntity = m_impl->m_sceneManager->createEntity(component->m_meshName.get(),  "General");
                thisEntity->setMaterialName("Test/ColourTest");

                sceneNode->setOrientation(component->m_transform.orientation);
                sceneNode->setScale(component->m_transform.scale);
                sceneNode->setPosition(component->m_transform.position);
                sceneNode->attachObject(thisEntity);

            }
            else if (component->m_meshName.get().size() > 0) {
                component->m_entity = m_impl->m_sceneManager->createEntity(component->m_meshName);
                sceneNode->attachObject(component->m_entity);
            }
            component->m_meshName.untouch();
        }
        if (component->m_visible.hasChanges()) {
            component->m_sceneNode->setVisible(component->m_visible.get());
            component->m_visible.untouch();
        }
        if (component->m_planeTexture.hasChanges()) {
            if (component->m_entity) {
                sceneNode->detachObject(component->m_entity);
                m_impl->m_sceneManager->destroyEntity(component->m_entity);
                component->m_entity = nullptr;
            }
            if (component->m_planeTexture.get().length() != 0) {
                Ogre::Plane plane(Ogre::Vector3::UNIT_Z, 0);
                std::string planeName("plane" + ++planeNameCounter);
                Ogre::MeshManager::getSingleton().createPlane(planeName,
                    Ogre::ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME,
                    plane, 10000, 10000);
                component->m_entity = m_impl->m_sceneManager->createEntity(
                    planeName
                );
                sceneNode->attachObject(component->m_entity);
                component->m_entity->setMaterialName(component->m_planeTexture.get());
                component->m_entity->setCastShadows(false);
            } else {
                component->m_meshName.touch();
            }
            component->m_planeTexture.untouch();
        }
        if(component->m_objectsToAttach.hasChanges()){
            for (auto obj : component->m_objectsToAttach.get()){
                component->_attachObject(obj);
                component->m_objectsToAttach.get().clear();
            }
            component->m_objectsToAttach.untouch();
        }
        if(component->m_attachToListener.hasChanges()){
            if (component->m_attachToListener.get()) {
                auto listener = SoundManager::getListener();

                if (OgreSceneNodeComponent::s_soundListenerAttached){
                    listener->detachFromNode();
                }

                else {
                    OgreSceneNodeComponent::s_soundListenerAttached = true;
                }

                listener->attachToNode(component->m_sceneNode);

            }
            component->m_attachToListener.untouch();
        }
        if (component->m_entity && component->m_entity->hasSkeleton()){
            // Progress animations
            Ogre::AnimationStateSet* animations = component->m_entity->getAllAnimationStates();
            if (component->m_animationChange) {
                component->m_animationChange = false;
                //Stop specific animations
                for (auto animationName : component->m_animationsToHalt) {
                    animations->getAnimationState(animationName)->setEnabled(false);
                }
                component->m_animationsToHalt.clear();
                // Start animations
                for (auto pair : component->m_animationsToStart) {
                    Ogre::AnimationState* animation = animations->getAnimationState(pair.first);
                    animation->setLoop(pair.second);
                    animation->setEnabled(true);
                }
                component->m_animationsToStart.clear();
            }
            // Progress animations and handle full animation halt
            Ogre::AnimationStateIterator iter = animations->getAnimationStateIterator();
            while (iter.hasMoreElements()){
                Ogre::AnimationState* animation = iter.getNext();
                animation->addTime(logicTime * 0.001 * component->m_animationSpeedFactor);
                if (component->m_fullAnimationHalt){
                    animation->setEnabled(false);
                }
            }
            component->m_fullAnimationHalt = false;
        }
    }
}

void OgreSceneNodeComponent::sendOrganelles(double x, double y)
{
    organellePositions.emplace_back(x,y,0);

//    MessageBox(NULL, std::to_string(x).c_str(), "sent organelles - x", 0);
//    MessageBox(NULL, std::to_string(y).c_str(), "sent organelles - y", 0);
}
