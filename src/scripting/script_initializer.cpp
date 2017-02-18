#include "scripting/script_initializer.h"

#include "engine/engine.h"
#include "engine/rng.h"
#include "game.h"
#include "scripting/luajit.h"
#include "scripting/wrapper_classes.h"
#include "engine/rolling_grid.h"




#include "engine/component.h"
#include "engine/component_factory.h"
#include "engine/engine.h"
#include "engine/entity.h"
#include "engine/game_state.h"
#include "engine/serialization.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "engine/player_data.h"
#include "engine/rng.h"

#include "bullet/bullet_ogre_conversion.h"
#include "bullet/bullet_to_ogre_system.h"
#include "bullet/collision_filter.h"
#include "bullet/collision_shape.h"
#include "bullet/collision_system.h"
#include "bullet/debug_drawing.h"
#include "bullet/rigid_body_system.h"
#include "bullet/update_physics_system.h"
#include "bullet/physical_world.h"

#include <utility>
#include <btBulletCollisionCommon.h>
#include <memory>
#include <OgreVector3.h>

#include "gui/script_wrappers.h"
#include "general/timed_life_system.h"
#include "general/locked_map.h"
#include "general/powerup_system.h"

#include "gui/CEGUIWindow.h"
#include "gui/CEGUIVideoPlayer.h"

#include "ogre/camera_system.h"
#include "ogre/colour_material.h"
#include "ogre/keyboard.h"
#include "ogre/light_system.h"
#include "ogre/mouse.h"
#include "ogre/render_system.h"
#include "ogre/scene_node_system.h"
#include "ogre/sky_system.h"

#include "ogre/workspace_system.h"


#include <OgreAxisAlignedBox.h>
#include <OgreColourValue.h>
#include <OgreMath.h>
#include <OgreMatrix3.h>
#include <OgreMaterialManager.h>
#include <OgreMaterial.h>
#include <OgreTechnique.h>
#include <OgreRay.h>
#include <OgreSceneManager.h>
#include <OgreSphere.h>
#include <OgreVector3.h>
#include <OgreSubEntity.h>
#include <OgreEntity.h>
#include <OgreSubMesh.h>

#include <string>


#include "microbe_stage/compound.h"
#include "microbe_stage/compound_absorber_system.h"
#include "microbe_stage/compound_emitter_system.h"
#include "microbe_stage/compound_registry.h"
#include "microbe_stage/bio_process_registry.h"
#include "microbe_stage/membrane_system.h"
#include "microbe_stage/compound_cloud_system.h"
#include "microbe_stage/process_system.h"
#include "microbe_stage/agent_cloud_system.h"
#include "microbe_stage/species_component.h"


#include "sound/sound_source_system.h"


#include <forward_list>
#include <iostream>

using namespace thrive;

static int
constructTraceback(
    lua_State* L
) {
    lua_Debug d;
    std::stringstream traceback;
    // Error message
    traceback << lua_tostring(L, -1) << ":" << std::endl;
    lua_pop(L, 1);
    // Stacktrace
    for (
        int stacklevel = 0;
        lua_getstack(L, stacklevel, &d);
        stacklevel++
    ) {
       lua_getinfo(L, "Sln", &d);
       traceback << "    " << d.short_src << ":" << d.currentline;
       if (d.name != nullptr) {
           traceback << " (" << d.namewhat << " " << d.name << ")";
       }
       traceback << std::endl;
    }
    lua_pushstring(L, traceback.str().c_str());
    std::cout << traceback.str().c_str() << std::endl;
    return 1;
}

/**
* @brief Thrive lua panic handler
*/
int thriveLuaPanic(lua_State* L);

int thriveLuaPanic(lua_State* L){

    const char* message = lua_tostring(L, -1);
    std::string err = message ? message :
        "An unexpected error occurred and forced the lua state to call atpanic";
    
    lua_Debug d;
    std::stringstream traceback;
    // Error message
    traceback << err << ":" << std::endl;
    lua_pop(L, 1);
    
    // Stacktrace
    for (
        int stacklevel = 0;
        lua_getstack(L, stacklevel, &d);
        stacklevel++
    ) {
        lua_getinfo(L, "Sln", &d);
        traceback << "    " << d.short_src << ":" << d.currentline;
        if (d.name != nullptr) {
            traceback << " (" << d.namewhat << " " << d.name << ")";
        }
        traceback << std::endl;
    }
    
    //lua_pushstring(L, traceback.str().c_str());

    // Print error //
    
    std::cout << "Lua panic! " << traceback.str() << std::endl;
    throw sol::error(traceback.str());
    return 1;
}



/**
* @brief Thrive lua error handler
*/
std::string thriveLuaOnError(sol::this_state lua);

std::string thriveLuaOnError(sol::this_state lua){

    lua_State* L = sol::state_view(lua).lua_state();

    const char* message = lua_tostring(L, -1);
    std::string err = message ? message :
        "An unexpected error occurred and forced the lua state to call atpanic";
    
    lua_Debug d;
    std::stringstream traceback;
    // Error message
    traceback << err << ":" << std::endl;
    lua_pop(L, 1);
    
    // Stacktrace
    for (
        int stacklevel = 0;
        lua_getstack(L, stacklevel, &d);
        stacklevel++
    ) {
        lua_getinfo(L, "Sln", &d);
        traceback << "    " << d.short_src << ":" << d.currentline;
        if (d.name != nullptr) {
            traceback << " (" << d.namewhat << " " << d.name << ")";
        }
        traceback << std::endl;
    }
    
    // Print error //
    std::cout << "Lua error detected! " << traceback.str() << std::endl;

    // Return as the error code
    return traceback.str();
}


//! \brief Binds all classes usable from Lua
//!
//! Needs to be called after global variables are bound.
//! \exception std::runtime_error if fails
void bindClassesToLua(sol::state &lua);

// Forward declare some binding functions
static void listboxItemBindings(sol::state &lua);
static void itemEntryluaBindings(sol::state &lua);
static void ogreLuaBindings(sol::state &lua);

void thrive::initializeLua(sol::state &lua){

    // Open lua modules //
    // see: http://www.lua.org/manual/5.3/manual.html#6 for documentation
    // about what these modules do
    lua.open_libraries(
        sol::lib::base,
        sol::lib::jit,
        
        sol::lib::debug,
        sol::lib::coroutine,
        sol::lib::string,
        sol::lib::math,
        sol::lib::table,
        sol::lib::package,
        sol::lib::io,
        sol::lib::os

        // These aren't currently used
        // sol::lib::bit32,
        // sol::lib::ffi
    );


    // Are these the same?
    //lua.set_panic
    lua.set_panic(&thriveLuaPanic);
    
    //luabind::set_pcall_callback(constructTraceback);

    // Class type registering //
    bindClassesToLua(lua);

    // Global objects //
    lua["Engine"] = &(Game::instance().engine());
    lua["rng"] = &(Game::instance().engine().rng());

    // Bind a custom traceback printer
    // Could probably also be print(debug.traceback())
    lua["thrivePanic"] = thriveLuaOnError;
}

void bindClassesToLua(sol::state &lua){




    // Engine bindings
    {
        StorageContainer::luaBindings(lua);
        StorageList::luaBindings(lua);
        System::luaBindings(lua);
        SystemWrapper::luaBindings(lua);
        Component::luaBindings(lua);
        ComponentFactory::luaBindings(lua);
        Entity::luaBindings(lua);
        Touchable::luaBindings(lua);
        GameState::luaBindings(lua);
        Engine::luaBindings(lua);
        RNG::luaBindings(lua);
        PlayerData::luaBindings(lua);
    }

    // General bindings
    {
        // Components
        TimedLifeComponent::luaBindings(lua);
        LockedMap::luaBindings(lua);
        PowerupComponent::luaBindings(lua);
        // Systems
        TimedLifeSystem::luaBindings(lua);
        PowerupSystem::luaBindings(lua);
        // Other
    }

    // Ogre bindings
    ogreLuaBindings(lua);

    // Bullet bindings
    {
        // Shapes
        CollisionShape::luaBindings(lua);
        BoxShape::luaBindings(lua);
        CapsuleShape::luaBindings(lua);
        CompoundShape::luaBindings(lua);
        ConeShape::luaBindings(lua);
        CylinderShape::luaBindings(lua);
        EmptyShape::luaBindings(lua);
        SphereShape::luaBindings(lua);
        // Components
        RigidBodyComponent::luaBindings(lua);
        CollisionComponent::luaBindings(lua);
        // Systems
        BulletToOgreSystem::luaBindings(lua);
        RigidBodyInputSystem::luaBindings(lua);
        RigidBodyOutputSystem::luaBindings(lua);
        BulletDebugDrawSystem::luaBindings(lua);
        UpdatePhysicsSystem::luaBindings(lua);
        CollisionSystem::luaBindings(lua);
        // Other
        PhysicalWorld::luaBindings(lua);
        CollisionFilter::luaBindings(lua);
        Collision::luaBindings(lua);
    }

    // Script bindings
    {
        
    }

    // Microbe stage bindings
    {
        // Components
        CompoundComponent::luaBindings(lua);
        ProcessorComponent::luaBindings(lua);
        CompoundBagComponent::luaBindings(lua);
        CompoundAbsorberComponent::luaBindings(lua);
        CompoundEmitterComponent::luaBindings(lua);
        TimedCompoundEmitterComponent::luaBindings(lua);
        MembraneComponent::luaBindings(lua);
        CompoundCloudComponent::luaBindings(lua);
        AgentCloudComponent::luaBindings(lua);
        SpeciesComponent::luaBindings(lua);
        // Systems
        CompoundMovementSystem::luaBindings(lua);
        CompoundAbsorberSystem::luaBindings(lua);
        CompoundEmitterSystem::luaBindings(lua);
        MembraneSystem::luaBindings(lua);
        CompoundCloudSystem::luaBindings(lua);
        ProcessSystem::luaBindings(lua);
        AgentCloudSystem::luaBindings(lua);
        // Other
        CompoundRegistry::luaBindings(lua);
        BioProcessRegistry::luaBindings(lua);
    }
    
    // Gui bindings
    {
        // Other
        listboxItemBindings(lua);
        itemEntryluaBindings(lua);
        CEGUIWindow::luaBindings(lua);
        CEGUIVideoPlayer::luaBindings(lua);

        StandardItemWrapper::luaBindings(lua);
    }

    // Sound bindings
    {
        Sound::luaBindings(lua);
        SoundSourceSystem::luaBindings(lua);
        SoundSourceComponent::luaBindings(lua);
    }
    
    RollingGrid::luaBindings(lua);
}



static void ListboxItem_setColour(
    CEGUI::ListboxTextItem &self,
    float r,
    float g,
    float b
) {
    self.setTextColours(CEGUI::Colour(r,g,b));
}

static void ListboxItem_setText(
    CEGUI::ListboxTextItem &self,
    const std::string& text
) {
    self.setText(text);
}

static void listboxItemBindings(sol::state &lua) {

    lua.new_usertype<CEGUI::ListboxTextItem>("ListboxItem",

        sol::constructors<sol::types<const std::string&>>(),
        
        "setTextColours", &ListboxItem_setColour,
        "setText", &ListboxItem_setText
    );
}

static void ItemEntry_setText(
    CEGUI::ItemEntry &self,
    const std::string& text
) {
    self.setText(text);
}

static bool ItemEntry_isSelected(
    CEGUI::ItemEntry &self
) {
    return self.isSelected();
}

static void ItemEntry_select(
    CEGUI::ItemEntry &self
) {
    self.select();
}

static void ItemEntry_deselect(
    CEGUI::ItemEntry &self
) {
    self.deselect();
}

static void ItemEntry_setSelectable(
    CEGUI::ItemEntry &self,
    bool setting
) {
    self.setSelectable(setting);
}

static void itemEntryluaBindings(sol::state &lua){

    lua.new_usertype<CEGUI::ItemEntry>("ItemEntry",

        sol::constructors<sol::types<const std::string&, const std::string&>>(),
        
        "isSelected", &ItemEntry_isSelected,
        "select", &ItemEntry_select,
        "deselect", &ItemEntry_deselect,
        "setSelectable", &ItemEntry_setSelectable,
        "setText", &ItemEntry_setText
    );
}

static void axisAlignedBoxBindings(sol::state &lua) {

    using namespace Ogre;

    lua.new_usertype<Ogre::AxisAlignedBox>("BoxShape",

        sol::constructors<sol::types<>, sol::types<Ogre::AxisAlignedBox::Extent>,
        sol::types<const Ogre::Vector3&, const Ogre::Vector3&>, sol::types<
        Ogre::Real, Ogre::Real, Ogre::Real,
        Ogre::Real, Ogre::Real, Ogre::Real >>(),

        sol::meta_function::equal_to, &Ogre::AxisAlignedBox::operator==,

        "Extent", sol::var(lua.create_table_with(
                "EXTENT_NULL", Ogre::AxisAlignedBox::EXTENT_NULL,
                "EXTENT_FINITE", Ogre::AxisAlignedBox::EXTENT_FINITE,
                "EXTENT_INFINITE", Ogre::AxisAlignedBox::EXTENT_INFINITE
            )),

        "CornerEnum", sol::var(lua.create_table_with(
                "FAR_LEFT_BOTTOM", Ogre::AxisAlignedBox::FAR_LEFT_BOTTOM,
                "FAR_LEFT_TOP", Ogre::AxisAlignedBox::FAR_LEFT_TOP,
                "FAR_RIGHT_TOP", Ogre::AxisAlignedBox::FAR_RIGHT_TOP,
                "FAR_RIGHT_BOTTOM", Ogre::AxisAlignedBox::FAR_RIGHT_BOTTOM,
                "NEAR_RIGHT_BOTTOM", Ogre::AxisAlignedBox::NEAR_RIGHT_BOTTOM,
                "NEAR_LEFT_BOTTOM", Ogre::AxisAlignedBox::NEAR_LEFT_BOTTOM,
                "NEAR_LEFT_TOP", Ogre::AxisAlignedBox::NEAR_LEFT_TOP,
                "NEAR_RIGHT_TOP", Ogre::AxisAlignedBox::NEAR_RIGHT_TOP
            )),
        
        "getMinimum",
            static_cast<const Vector3& (AxisAlignedBox::*) () const>(
                &AxisAlignedBox::getMinimum),
        
        "getMaximum",
            static_cast<const Vector3& (AxisAlignedBox::*) () const>(
                &AxisAlignedBox::getMaximum),

        "setMinimum", sol::overload(
            static_cast<void (AxisAlignedBox::*) (const Vector3&)>(
                &AxisAlignedBox::setMinimum),
            static_cast<void (AxisAlignedBox::*) (Real, Real, Real)>(
                &AxisAlignedBox::setMinimum)),

        "setMinimumX", &AxisAlignedBox::setMinimumX,
        "setMinimumY", &AxisAlignedBox::setMinimumY,
        "setMinimumZ", &AxisAlignedBox::setMinimumZ,
        
        "setMaximum", sol::overload(
            static_cast<void (AxisAlignedBox::*) (const Vector3&)>(
                &AxisAlignedBox::setMaximum),
            static_cast<void (AxisAlignedBox::*) (Real, Real, Real)>(
                &AxisAlignedBox::setMaximum)),
        
        "setMaximumX", &AxisAlignedBox::setMaximumX,
        "setMaximumY", &AxisAlignedBox::setMaximumY,
        "setMaximumZ", &AxisAlignedBox::setMaximumZ,
        "setExtents", sol::overload(
            static_cast<void (AxisAlignedBox::*) (const Vector3&,
                const Vector3&)>(&AxisAlignedBox::setExtents),
            static_cast<void (AxisAlignedBox::*) (Real, Real, Real, Real, Real,
                Real)>(&AxisAlignedBox::setExtents)),

        "getCorner", &AxisAlignedBox::getCorner,
        "merge", sol::overload(
            static_cast<void (AxisAlignedBox::*) (const AxisAlignedBox&)>(
                &AxisAlignedBox::merge),
            static_cast<void (AxisAlignedBox::*) (const Vector3&)>(
                &AxisAlignedBox::merge)),

        "setNull", &AxisAlignedBox::setNull,
        "isNull", &AxisAlignedBox::isNull,
        "isFinite", &AxisAlignedBox::isFinite,
        "setInfinite", &AxisAlignedBox::setInfinite,
        "isInfinite", &AxisAlignedBox::isInfinite,
        "intersects", sol::overload(
            static_cast<bool (AxisAlignedBox::*) (const AxisAlignedBox&) const>(
                &AxisAlignedBox::intersects),
            static_cast<bool (AxisAlignedBox::*) (const Sphere&) const>(
                &AxisAlignedBox::intersects),
            static_cast<bool (AxisAlignedBox::*) (const Plane&) const>(
                &AxisAlignedBox::intersects),
            static_cast<bool (AxisAlignedBox::*) (const Vector3&) const>(
                &AxisAlignedBox::intersects)),
        
        "intersection", &AxisAlignedBox::intersection,
        "volume", &AxisAlignedBox::volume,
        "scale", &AxisAlignedBox::scale,

        "getCenter", &AxisAlignedBox::getCenter,
        "getSize", &AxisAlignedBox::getSize,
        "getHalfSize", &AxisAlignedBox::getHalfSize,
        "contains", sol::overload(
            static_cast<bool (AxisAlignedBox::*) (const Vector3&) const>(
                &AxisAlignedBox::contains),
            static_cast<bool (AxisAlignedBox::*) (const AxisAlignedBox&) const>(
                &AxisAlignedBox::contains)),

        "distance", &AxisAlignedBox::distance
    );
}

static void colourValueBindings(sol::state &lua) {

    using namespace Ogre;

    lua.new_usertype<ColourValue>("ColourValue",

        sol::constructors<sol::types<float, float, float, float>>(),

        sol::meta_function::equal_to, &ColourValue::operator==,

        sol::meta_function::addition, &ColourValue::operator+,


        sol::meta_function::multiplication, sol::overload(
            static_cast<ColourValue (ColourValue::*)(const ColourValue&) const>(
                &ColourValue::operator*),
            static_cast<ColourValue (ColourValue::*)(const float) const>(
                &ColourValue::operator*)
        ),

        sol::meta_function::subtraction, &ColourValue::operator-,
        
        "saturate", &ColourValue::saturate,
        "setHSB", &ColourValue::setHSB,
        "getHSB", &ColourValue::getHSB,
        
        "r", &ColourValue::r,
        "g", &ColourValue::g,
        "b", &ColourValue::b,
        "a", &ColourValue::a
    );
}

static void degreeBindings(sol::state &lua) {

     lua.new_usertype<Ogre::Degree>("Degree",

         sol::constructors<sol::types<Ogre::Real>, sol::types<const Ogre::Radian&>>(),

         sol::meta_function::equal_to, &Ogre::Degree::operator==,

         sol::meta_function::less_than, &Ogre::Degree::operator<,

         sol::meta_function::addition, static_cast<Ogre::Degree (Ogre::Degree::*)(
             const Ogre::Degree&) const>(&Ogre::Degree::operator+),

         sol::meta_function::subtraction, static_cast<Ogre::Degree (Ogre::Degree::*)(
             const Ogre::Degree&) const>(&Ogre::Degree::operator-),

         sol::meta_function::multiplication, sol::overload(
             static_cast<Ogre::Degree (Ogre::Degree::*)(const Ogre::Degree&) const>(
                 &Ogre::Degree::operator*),
             static_cast<Ogre::Degree (Ogre::Degree::*)(const Ogre::Real) const>(
                 &Ogre::Degree::operator*)
         ),

         sol::meta_function::division, &Ogre::Degree::operator/,
         
         "valueDegrees", &Ogre::Degree::valueDegrees
     );
}


static void
    SubEntity_setColour(
        Ogre::SubEntity &self,
        const Ogre::ColourValue& colour
    ) {
    auto material = thrive::getColourMaterial(colour);
    self.setMaterial(material);
}

static void
    Entity_setColour(
        Ogre::Entity &self,
        const Ogre::ColourValue& colour
    ) {
    auto material = thrive::getColourMaterial(colour);
    self.setMaterial(material);
}

static void
    SubEntity_setMaterial(
        Ogre::SubEntity &self,
        const Ogre::String& name
    ) {
    Ogre::MaterialManager& manager = Ogre::MaterialManager::getSingleton();
    Ogre::MaterialPtr material = manager.getByName(
        name
    );
    self.setMaterial(material);
}

static void
    Entity_setMaterial(
        Ogre::Entity &self,
        const Ogre::String& name
    ) {
    Ogre::MaterialManager& manager = Ogre::MaterialManager::getSingleton();
    Ogre::MaterialPtr material = manager.getByName(
        name
    );
    self.setMaterial(material);
}

static void
    SubEntity_tintColour(
        Ogre::SubEntity &self,
        const Ogre::String& groupName,
        const Ogre::String& materialName,
        const Ogre::ColourValue& colour
    ) {
    Ogre::MaterialPtr baseMaterial = Ogre::MaterialManager::getSingleton().getByName(materialName);
    Ogre::MaterialPtr materialPtr = baseMaterial->clone(groupName);
    materialPtr->compile();
    Ogre::TextureUnitState* ptus = materialPtr->getTechnique(0)->getPass(0)->getTextureUnitState(0);
    ptus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL, Ogre::LBS_TEXTURE, colour);
    self.setMaterial(materialPtr);
}

static void
    Entity_tintColour(
        Ogre::Entity &self,
        const Ogre::String& materialName,
        const Ogre::ColourValue& colour
    ) {
    Ogre::MaterialPtr baseMaterial = Ogre::MaterialManager::getSingleton().getByName(materialName);
    Ogre::MaterialPtr materialPtr = baseMaterial->clone(materialName + std::to_string(static_cast<int>(colour.r*256))
        + std::to_string(static_cast<int>(colour.g*256)) + std::to_string(static_cast<int>(colour.b*256)));
    materialPtr->compile();
    Ogre::TextureUnitState* ptus = materialPtr->getTechnique(0)->getPass(0)->getTextureUnitState(0);
    ptus->setAlphaOperation(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL, Ogre::LBS_TEXTURE, colour.a);
    ptus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL, Ogre::LBS_TEXTURE, colour);
    self.setMaterial(materialPtr);
}

static int clonedIndex = 0;

static void
    Entity_cloneMaterial(
        Ogre::Entity &self,
        const Ogre::String& materialName
    ) {
    Ogre::MaterialPtr baseMaterial = Ogre::MaterialManager::getSingleton().getByName(materialName);
    Ogre::MaterialPtr materialPtr = baseMaterial->clone(materialName + std::to_string(clonedIndex));
    materialPtr->compile();
    self.setMaterialName(materialName + std::to_string(clonedIndex));
    clonedIndex++;
}

static void
    Entity_setMaterialColour(
        Ogre::Entity &self,
        //const String& materialName,
        const Ogre::ColourValue& colour
    ) {
    //Ogre::MaterialPtr baseMaterial = Ogre::MaterialManager::getSingleton().getByName(materialName);
    //Ogre::MaterialPtr materialPtr = baseMaterial->clone(materialName + std::to_string(clonedIndex));
    //materialPtr->compile();
    //self.setMaterialName(materialName + std::to_string(clonedIndex));
    //clonedIndex++;

    Ogre::SubMesh* sub = self.getMesh()->getSubMesh(0);
    Ogre::MaterialPtr materialPtr = Ogre::MaterialManager::getSingleton().getByName(sub->getMaterialName());
    Ogre::TextureUnitState* ptus = materialPtr->getTechnique(0)->getPass(0)->getTextureUnitState(0);
    ptus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL, Ogre::LBS_TEXTURE, colour);
    ptus->setAlphaOperation(Ogre::LBX_MODULATE, Ogre::LBS_MANUAL, Ogre::LBS_TEXTURE, colour.a);
    //self.setMaterial(materialPtr);
}

static void ogreEntityBindings(sol::state &lua) {

    lua.new_usertype<Ogre::SubEntity>("OgreSubEntity",

        "setColour", &SubEntity_setColour,
        "setMaterial", &SubEntity_setMaterial,
        "tintColour", &SubEntity_tintColour
    );

    lua.new_usertype<Ogre::Entity>("OgreEntity",

        "getSubEntity", static_cast<Ogre::SubEntity*(Ogre::Entity::*)(const Ogre::String&)>(
            &Ogre::Entity::getSubEntity),
        "getNumSubEntities", &Ogre::Entity::getNumSubEntities,
        "setColour", &Entity_setColour,
        "setMaterial", &Entity_setMaterial,
        "cloneMaterial", &Entity_cloneMaterial,
        "setMaterialColour", &Entity_setMaterialColour,
        "tintColour", &Entity_tintColour
    );

    lua.new_usertype<Ogre::MovableObject>("MovableObject",

        sol::base_classes, sol::bases<Ogre::Entity>()
    );
}

static void matrix3Bindings(sol::state &lua) {

    using namespace Ogre;

    lua.new_usertype<Ogre::Matrix3>("Matrix3",
        
        sol::constructors<sol::types<>, sol::types<
        Ogre::Real, Ogre::Real, Ogre::Real,
        Ogre::Real, Ogre::Real, Ogre::Real,
        Ogre::Real, Ogre::Real, Ogre::Real>>(),

        sol::meta_function::equal_to, &Ogre::Matrix3::operator==,

        sol::meta_function::addition, &Ogre::Matrix3::operator+,

        sol::meta_function::subtraction, static_cast<Ogre::Matrix3 (Ogre::Matrix3::*)(
            const Ogre::Matrix3&) const>(&Ogre::Matrix3::operator-),

        sol::meta_function::multiplication, sol::overload(
            static_cast<Ogre::Matrix3 (Ogre::Matrix3::*)(const Ogre::Matrix3&) const>(
                &Ogre::Matrix3::operator*),
            static_cast<Ogre::Matrix3 (Ogre::Matrix3::*)(const Ogre::Real) const>(
                &Ogre::Matrix3::operator*),
            static_cast<Ogre::Vector3 (Ogre::Matrix3::*)(const Ogre::Vector3&) const>(
                &Ogre::Matrix3::operator*)
        ),

        "GetColumn", &Matrix3::GetColumn,
        "SetColumn", &Matrix3::SetColumn,
        "FromAxes", &Matrix3::FromAxes,
        "Transpose", &Matrix3::Transpose,
        "Inverse",
        static_cast<bool(Matrix3::*)(Matrix3&, Real) const>(&Matrix3::Inverse),

        "Determinant", &Matrix3::Determinant,
        "SingularValueDecomposition", &Matrix3::SingularValueDecomposition,
        "SingularValueComposition", &Matrix3::SingularValueComposition,
        "Orthonormalize", &Matrix3::Orthonormalize,
        "QDUDecomposition", &Matrix3::QDUDecomposition,
        "SpectralNorm", &Matrix3::SpectralNorm,
        "ToAngleAxis", static_cast<void(Matrix3::*)(Vector3&, Radian&) const>(
            &Matrix3::ToAngleAxis),
        
        "FromAngleAxis", &Matrix3::FromAngleAxis,
        "ToEulerAnglesXYZ", &Matrix3::ToEulerAnglesXYZ,
        "ToEulerAnglesXZY", &Matrix3::ToEulerAnglesXZY,
        "ToEulerAnglesYXZ", &Matrix3::ToEulerAnglesYXZ,
        "ToEulerAnglesYZX", &Matrix3::ToEulerAnglesYZX,
        "ToEulerAnglesZXY", &Matrix3::ToEulerAnglesZXY,
        "ToEulerAnglesZYX", &Matrix3::ToEulerAnglesZYX,
        "FromEulerAnglesXYZ", &Matrix3::FromEulerAnglesXYZ,
        "FromEulerAnglesXZY", &Matrix3::FromEulerAnglesXZY,
        "FromEulerAnglesYXZ", &Matrix3::FromEulerAnglesYXZ,
        "FromEulerAnglesYZX", &Matrix3::FromEulerAnglesYZX,
        "FromEulerAnglesZXY", &Matrix3::FromEulerAnglesZXY,
        "FromEulerAnglesZYX", &Matrix3::FromEulerAnglesZYX,
        "hasScale", &Matrix3::hasScale
    );
}

static void planeBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Plane>("Plane",

        sol::constructors<sol::types<>, sol::types<const Vector3&, Real>,
        sol::types<Real, Real, Real, Real>, sol::types<const Vector3&, const Vector3&>,
        sol::types<const Vector3&, const Vector3&, const Vector3&>>(),

        //sol::meta_function::equal_to, &Ogre::Plane::operator==,

        "Side", sol::var(lua.create_table_with(
                "NO_SIDE", Plane::NO_SIDE,
                "POSITIVE_SIDE", Plane::POSITIVE_SIDE,
                "NEGATIVE_SIDE", Plane::NEGATIVE_SIDE,
                "BOTH_SIDE", Plane::BOTH_SIDE
            )),

        "getSide", sol::overload(
            static_cast<Plane::Side (Plane::*) (const Vector3&) const>(&Plane::getSide),
            static_cast<Plane::Side (Plane::*) (const AxisAlignedBox&) const>(&Plane::getSide),
            static_cast<Plane::Side (Plane::*) (const Vector3&, const Vector3&) const>(
                &Plane::getSide)
        ),

        "getDistance", &Plane::getDistance,

        "redefine", sol::overload(
            static_cast<void (Plane::*) (const Vector3&, const Vector3&)>(&Plane::redefine),
            static_cast<void (Plane::*) (const Vector3&, const Vector3&, const Vector3&)>(
                &Plane::redefine)
        ),

        "projectVector", &Plane::projectVector,
        "normalise", &Plane::normalise,
        "normal", &Plane::normal,
        "d", &Plane::d
    );
}

static void quaternionBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Quaternion>("Quaternion",

        sol::constructors<sol::types<>, sol::types<const Matrix3&>,
        sol::types<Real, Real, Real, Real>, sol::types<const Radian&, const Vector3&>,
        sol::types<const Vector3&, const Vector3&, const Vector3&>>(),

        //sol::meta_function::equal_to, &Ogre::Quaternion::operator==,

        sol::meta_function::addition, &Ogre::Quaternion::operator+,

        sol::meta_function::subtraction, static_cast<Ogre::Quaternion (Ogre::Quaternion::*)(
            const Ogre::Quaternion&) const>(&Ogre::Quaternion::operator-),

        sol::meta_function::multiplication, sol::overload(
            static_cast<Ogre::Quaternion (Ogre::Quaternion::*)(const Ogre::Quaternion&) const>(
                &Ogre::Quaternion::operator*),
            static_cast<Ogre::Quaternion (Ogre::Quaternion::*)(const Ogre::Real) const>(
                &Ogre::Quaternion::operator*),
            static_cast<Ogre::Vector3 (Ogre::Quaternion::*)(const Ogre::Vector3&) const>(
                &Ogre::Quaternion::operator*)
        ),
        
        "FromRotationMatrix", &Quaternion::FromRotationMatrix,
        "ToRotationMatrix", &Quaternion::ToRotationMatrix,
        "FromAngleAxis", &Quaternion::FromAngleAxis,
        "ToAngleAxis", static_cast<void(Quaternion::*)(Radian&, Vector3&) const>(
            &Quaternion::ToAngleAxis),
        "FromAxes", static_cast<void(Quaternion::*)(const Vector3&, const Vector3&,
            const Vector3&)>(&Quaternion::FromAxes),
        "ToAxes", static_cast<void(Quaternion::*)(Vector3&, Vector3&, Vector3&) const>(
            &Quaternion::ToAxes),
        "xAxis", &Quaternion::xAxis,
        "yAxis", &Quaternion::yAxis,
        "zAxis", &Quaternion::zAxis,
        "Dot", &Quaternion::Dot,
        "Norm", &Quaternion::Norm,
        "normalise", &Quaternion::normalise,
        "Inverse", &Quaternion::Inverse,
        "UnitInverse", &Quaternion::UnitInverse,
        "Exp", &Quaternion::Exp,
        "Log", &Quaternion::Log,
        "getRoll", &Quaternion::getRoll,
        "getPitch", &Quaternion::getPitch,
        "getYaw", &Quaternion::getYaw,
        "equals", &Quaternion::equals,
        "isNaN", &Quaternion::isNaN
    );
}


static void radianBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Radian>("Radian",

        sol::constructors<sol::types<Real>, sol::types<const Degree&>>(),

        //sol::meta_function::equal_to, &Ogre::Radian::operator==,

        sol::meta_function::less_than, &Ogre::Radian::operator<,

        sol::meta_function::addition, static_cast<Ogre::Radian (Ogre::Radian::*)(
            const Ogre::Radian&) const>(&Ogre::Radian::operator+),

        sol::meta_function::subtraction, static_cast<Ogre::Radian (Ogre::Radian::*)(
            const Ogre::Radian&) const>(&Ogre::Radian::operator-),

        sol::meta_function::multiplication, sol::overload(
            static_cast<Ogre::Radian (Ogre::Radian::*)(const Ogre::Radian&) const>(
                &Ogre::Radian::operator*),
            static_cast<Ogre::Radian (Ogre::Radian::*)(const Ogre::Real) const>(
                &Ogre::Radian::operator*)
        ),

        sol::meta_function::division, static_cast<Ogre::Radian (Ogre::Radian::*)(
            const Ogre::Real) const>(&Ogre::Radian::operator/),
        
        "valueDegrees", &Radian::valueDegrees,
        "valueRadians", &Radian::valueRadians,
        "valueAngleUnits", &Radian::valueAngleUnits
    );
}

static void rayBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Ray>("Ray",

        sol::constructors<sol::types<>, sol::types<const Vector3&, const Vector3&>>(),

        sol::meta_function::multiplication, static_cast<Ogre::Vector3 (Ogre::Ray::*)(
            Ogre::Real) const>(&Ogre::Ray::operator*),

        "setOrigin", &Ray::setOrigin,
        "getOrigin", &Ray::getOrigin,
        "setDirection", &Ray::setDirection,
        "getDirection", &Ray::getDirection,
        "getPoint", &Ray::getPoint,
        // returns a tuple now
        "intersects", static_cast<std::pair<bool, Ogre::Real> (Ogre::Ray::*)(
            const Ogre::Plane&) const>(&Ray::intersects)
        );
}


static void sceneManagerBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::SceneManager>("SceneManager",

        "PrefabType", sol::var(lua.create_table_with(
                "PT_PLANE", SceneManager::PT_PLANE,
                "PT_CUBE", SceneManager::PT_CUBE,
                "PT_SPHERE", SceneManager::PT_SPHERE
            )),
        
        "setAmbientLight", &SceneManager::setAmbientLight
    );
}

static void sphereBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Sphere>("Sphere",

        sol::constructors<sol::types<>, sol::types<const Vector3&, Real>>(),

        "getRadius", &Sphere::getRadius,
        "setRadius", &Sphere::setRadius,
        "getCenter", &Sphere::getCenter,
        "setCenter", &Sphere::setCenter,
        
        "intersects", sol::overload(
            static_cast<bool (Sphere::*)(const Sphere&) const>(&Sphere::intersects),
            static_cast<bool (Sphere::*)(const AxisAlignedBox&) const>(&Sphere::intersects),
            static_cast<bool (Sphere::*)(const Plane&) const>(&Sphere::intersects),
            static_cast<bool (Sphere::*)(const Vector3&) const>(&Sphere::intersects)
        ),
        
        "merge", &Sphere::merge
    );
}

static void vector3Bindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Vector3>("Vector3",

        sol::constructors<sol::types<>, sol::types<const Real, const Real, const Real>>(),

        //sol::meta_function::equal_to, &Ogre::Vector3::operator==,

        sol::meta_function::less_than, &Ogre::Vector3::operator <,
        
        sol::meta_function::addition, static_cast<Ogre::Vector3 (Ogre::Vector3::*)(
            const Ogre::Vector3&) const>(&Ogre::Vector3::operator+),

        sol::meta_function::subtraction, static_cast<Ogre::Vector3 (Ogre::Vector3::*)(
            const Ogre::Vector3&) const>(&Ogre::Vector3::operator-),

        sol::meta_function::multiplication, sol::overload(
            static_cast<Ogre::Vector3 (Ogre::Vector3::*)(const Ogre::Vector3&) const>(
                &Ogre::Vector3::operator*),
            static_cast<Ogre::Vector3 (Ogre::Vector3::*)(const Ogre::Real) const>(
                &Ogre::Vector3::operator*)
        ),

        
        sol::meta_function::division, sol::overload(
            static_cast<Ogre::Vector3 (Ogre::Vector3::*)(const Ogre::Vector3&) const>(
                &Ogre::Vector3::operator/),
            static_cast<Ogre::Vector3 (Ogre::Vector3::*)(const Ogre::Real) const>(
                &Ogre::Vector3::operator/)
        ),

        //.def(tostring(self))

        "x", &Vector3::x,
        "y", &Vector3::y,
        "z", &Vector3::z,
        
        "length", &Vector3::length,
        "squaredLength", &Vector3::squaredLength,
        "distance", &Vector3::distance,
        "squaredDistance", &Vector3::squaredDistance,
        "dotProduct", &Vector3::dotProduct,
        "absDotProduct", &Vector3::absDotProduct,
        "normalise", &Vector3::normalise,
        "crossProduct", &Vector3::crossProduct,
        "midPoint", &Vector3::midPoint,
        "makeFloor", &Vector3::makeFloor,
        "makeCeil", &Vector3::makeCeil,
        "perpendicular", &Vector3::perpendicular,
        "randomDeviant", &Vector3::randomDeviant,
        "angleBetween", &Vector3::angleBetween,
        "getRotationTo", &Vector3::getRotationTo,
        "isZeroLength", &Vector3::isZeroLength,
        "normalisedCopy", &Vector3::normalisedCopy,
        "reflect", &Vector3::reflect,
        "positionEquals", &Vector3::positionEquals,
        "positionCloses", &Vector3::positionCloses,
        "directionEquals", &Vector3::directionEquals,
        "isNaN", &Vector3::isNaN,
        "primaryAxis", &Vector3::primaryAxis
    );
}
    
static void ogreLuaBindings(sol::state &lua){
    
    // Math
    axisAlignedBoxBindings(lua);
    colourValueBindings(lua);
    degreeBindings(lua);
    matrix3Bindings(lua);
    planeBindings(lua);
    quaternionBindings(lua);
    radianBindings(lua);
    rayBindings(lua);
    sphereBindings(lua);
    vector3Bindings(lua);
    // Scene Manager
    sceneManagerBindings(lua);
    ogreEntityBindings(lua);
    // Components
    OgreCameraComponent::luaBindings(lua);
    OgreLightComponent::luaBindings(lua);
    OgreSceneNodeComponent::luaBindings(lua);
    SkyPlaneComponent::luaBindings(lua);
    OgreWorkspaceComponent::luaBindings(lua);
    // Systems
    OgreAddSceneNodeSystem::luaBindings(lua);
    OgreCameraSystem::luaBindings(lua);
    OgreLightSystem::luaBindings(lua);
    OgreRemoveSceneNodeSystem::luaBindings(lua);
    OgreUpdateSceneNodeSystem::luaBindings(lua);
    thrive::RenderSystem::luaBindings(lua); // Fully qualified because of Ogre::RenderSystem
    SkySystem::luaBindings(lua);
    OgreWorkspaceSystem::luaBindings(lua);
    // Other
    Keyboard::luaBindings(lua);
    Mouse::luaBindings(lua);
}


