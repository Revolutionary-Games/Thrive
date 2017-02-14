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

    // Class type registering //
    bindClassesToLua(lua);

    // Global objects //
    lua["Engine"] = &(Game::instance().engine());
    lua["rng"] = &(Game::instance().engine().rng());    
}

void bindClassesToLua(sol::state &lua){

    // Are these the same?
    //lua.set_panic
    //luabind::set_pcall_callback(constructTraceback);


    // Engine bindings
    {
        StorageContainer::luaBindings(lua);
        StorageList::luaBindings(lua);
        System::luaBindings(lua);
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

        sol::base_classes, sol::bases<Component>(),
        
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

static bool
    Ray_intersects(
        const Ogre::Ray &self,
        const Ogre::Plane& plane,
        Ogre::Real& t
    ) {
    bool intersects = false;
    std::tie(intersects, t) = self.intersects(plane);
    return intersects;
}

static void radianBindings(sol::state &lua) {

    using namespace Ogre;
    
    lua.new_usertype<Ogre::Ray>("Ray",

        sol::constructors<sol::types<>, sol::types<const Vector3&, const Vector3&>>(),

        sol::meta_function::multiplication, static_cast<Ogre::Radian (Ogre::Radian::*)(
            Ogre::Real) const>(&Ogre::Radian::operator*),

        "setOrigin", &Ray::setOrigin,
        "getOrigin", &Ray::getOrigin,
        "setDirection", &Ray::setDirection,
        "getDirection", &Ray::getDirection,
        "getPoint", &Ray::getPoint,
        "intersects", &Ray_intersects
        );
}

static luabind::scope
    sceneManagerBindings() {
    return class_<SceneManager>("SceneManager")
        .enum_("PrefabType") [
            value("PT_PLANE", SceneManager::PT_PLANE),
            value("PT_CUBE", SceneManager::PT_CUBE),
            value("PT_SPHERE", SceneManager::PT_SPHERE)
        ]
        // Fails to compile after upgrade to 2.0
        // .def("createEntity",
        //     static_cast<Entity* (SceneManager::*)(
        //         const Ogre::String&)>(&SceneManager::createEntity)
        // )
        // .def("createEntity",
        //     static_cast<Entity* (SceneManager::*)(
        //         SceneManager::PrefabType)>(&SceneManager::createEntity)
        // )
        .def("setAmbientLight", &SceneManager::setAmbientLight)
        ;
}


static luabind::scope
    sphereBindings() {
    return class_<Sphere>("Sphere")
        .def(constructor<>())
        .def(constructor<const Vector3&, Real>())
        .def("getRadius", &Sphere::getRadius)
        .def("setRadius", &Sphere::setRadius)
        .def("getCenter", &Sphere::getCenter)
        .def("setCenter", &Sphere::setCenter)
        .def("intersects",
            static_cast<bool (Sphere::*)(const Sphere&) const>(&Sphere::intersects)
        )
        .def("intersects",
            static_cast<bool (Sphere::*)(const AxisAlignedBox&) const>(&Sphere::intersects)
        )
        .def("intersects",
            static_cast<bool (Sphere::*)(const Plane&) const>(&Sphere::intersects)
        )
        .def("intersects",
            static_cast<bool (Sphere::*)(const Vector3&) const>(&Sphere::intersects)
        )
        .def("merge", &Sphere::merge)
        ;
}


static luabind::scope
    vector3Bindings() {
    return class_<Vector3>("Vector3")
        .def(constructor<>())
        .def(constructor<const Real, const Real, const Real>())
        .def(const_self == other<Vector3>())
        .def(const_self + other<Vector3>())
        .def(const_self - other<Vector3>())
        .def(const_self * Real())
        .def(Real() * const_self)
        .def(const_self * other<Vector3>())
        .def(const_self / Real())
        .def(const_self / other<Vector3>())
        .def(const_self < other<Vector3>())
        .def(tostring(self))
        .def_readwrite("x", &Vector3::x)
        .def_readwrite("y", &Vector3::y)
        .def_readwrite("z", &Vector3::z)
        .def("length", &Vector3::length)
        .def("squaredLength", &Vector3::squaredLength)
        .def("distance", &Vector3::distance)
        .def("squaredDistance", &Vector3::squaredDistance)
        .def("dotProduct", &Vector3::dotProduct)
        .def("absDotProduct", &Vector3::absDotProduct)
        .def("normalise", &Vector3::normalise)
        .def("crossProduct", &Vector3::crossProduct)
        .def("midPoint", &Vector3::midPoint)
        .def("makeFloor", &Vector3::makeFloor)
        .def("makeCeil", &Vector3::makeCeil)
        .def("perpendicular", &Vector3::perpendicular)
        .def("randomDeviant", &Vector3::randomDeviant)
        .def("angleBetween", &Vector3::angleBetween)
        .def("getRotationTo", &Vector3::getRotationTo)
        .def("isZeroLength", &Vector3::isZeroLength)
        .def("normalisedCopy", &Vector3::normalisedCopy)
        .def("reflect", &Vector3::reflect)
        .def("positionEquals", &Vector3::positionEquals)
        .def("positionCloses", &Vector3::positionCloses)
        .def("directionEquals", &Vector3::directionEquals)
        .def("isNaN", &Vector3::isNaN)
        .def("primaryAxis", &Vector3::primaryAxis)
        ;
}

static void ogreLuaBindings(sol::state &lua){

    // Math
    axisAlignedBoxBindings(),
        colourValueBindings(),
        degreeBindings(),
        matrix3Bindings(),
        planeBindings(),
        quaternionBindings(),
        radianBindings(),
        rayBindings(),
        sphereBindings(),
        vector3Bindings(),
        // Scene Manager
        sceneManagerBindings(),
        movableObjectBindings(),
        entityBindings(),
        // Components
        OgreCameraComponent::luaBindings(),
        OgreLightComponent::luaBindings(),
        OgreSceneNodeComponent::luaBindings(),
        SkyPlaneComponent::luaBindings(),
        OgreWorkspaceComponent::luaBindings(),
        // Systems
        OgreAddSceneNodeSystem::luaBindings(),
        OgreCameraSystem::luaBindings(),
        OgreLightSystem::luaBindings(),
        OgreRemoveSceneNodeSystem::luaBindings(),
        OgreUpdateSceneNodeSystem::luaBindings(),
        thrive::RenderSystem::luaBindings(), // Fully qualified because of Ogre::RenderSystem
        SkySystem::luaBindings(),
        OgreWorkspaceSystem::luaBindings(),
        // Other
        Keyboard::luaBindings(),
        Mouse::luaBindings()
}


