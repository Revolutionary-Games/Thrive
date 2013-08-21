#include "ogre/text_overlay.h"

#include "engine/component_registry.h"
#include "engine/engine.h"
#include "engine/entity_filter.h"
#include "scripting/luabind.h"

#include <iostream>
#include <OgreOverlayManager.h>

using namespace thrive;

////////////////////////////////////////////////////////////////////////////////
// TextOverlayComponent
////////////////////////////////////////////////////////////////////////////////


luabind::scope
TextOverlayComponent::luaBindings() {
    using namespace luabind;
    return class_<TextOverlayComponent, Component>("TextOverlayComponent")
        .enum_("HorizontalAlignment") [
            value("Left", Ogre::GHA_LEFT),
            value("Center", Ogre::GHA_CENTER),
            value("Right", Ogre::GHA_RIGHT)
        ]
        .enum_("VerticalAlignment") [
            value("Top", Ogre::GVA_TOP),
            value("Center", Ogre::GVA_CENTER),
            value("Bottom", Ogre::GVA_BOTTOM)
        ]
        .scope [
            def("TYPE_NAME", &TextOverlayComponent::TYPE_NAME),
            def("TYPE_ID", &TextOverlayComponent::TYPE_ID),
            class_<Properties, Touchable>("Properties")
                .def_readwrite("charHeight", &Properties::charHeight)
                .def_readwrite("colour", &Properties::colour)
                .def_readwrite("fontName", &Properties::fontName)
                .def_readwrite("height", &Properties::height)
                .def_readwrite("horizontalAlignment", &Properties::horizontalAlignment)
                .def_readwrite("left", &Properties::left)
                .def_readwrite("text", &Properties::text)
                .def_readwrite("top", &Properties::top)
                .def_readwrite("verticalAlignment", &Properties::verticalAlignment)
                .def_readwrite("width", &Properties::width)
        ]
        .def(constructor<std::string>())
        .def_readonly("name", &TextOverlayComponent::m_name)
        .def_readonly("properties", &TextOverlayComponent::m_properties)
    ;
}

TextOverlayComponent::TextOverlayComponent(
    Ogre::String name
) : m_name(name)
{
}

REGISTER_COMPONENT(TextOverlayComponent)


////////////////////////////////////////////////////////////////////////////////
// TextOverlaySystem
////////////////////////////////////////////////////////////////////////////////

struct TextOverlaySystem::Implementation {

    Implementation() {
        m_overlayManager = Ogre::OverlayManager::getSingletonPtr();
        m_overlay = m_overlayManager->create("text_overlay");
        m_panel = static_cast<Ogre::OverlayContainer*>(
            m_overlayManager->createOverlayElement("Panel", "text_panel")
        );
        m_panel->setDimensions(1.0, 1.0);
        m_panel->setPosition(0.0, 0.0);
        m_overlay->add2D(m_panel);
    }

    EntityFilter<
        TextOverlayComponent
    > m_entities = {true};

    Ogre::Overlay* m_overlay = nullptr;

    Ogre::OverlayManager* m_overlayManager = nullptr;

    Ogre::OverlayContainer* m_panel = nullptr;

    std::unordered_map<EntityId, Ogre::TextAreaOverlayElement*> m_textOverlays;
};


TextOverlaySystem::TextOverlaySystem()
  : m_impl(new Implementation())
{
}


TextOverlaySystem::~TextOverlaySystem() {}


void
TextOverlaySystem::init(
    Engine* engine
) {
    System::init(engine);
    m_impl->m_entities.setEntityManager(&engine->entityManager());
    m_impl->m_overlay->show();
}


void
TextOverlaySystem::shutdown() {
    m_impl->m_overlay->hide();
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
TextOverlaySystem::update(int) {
    for (auto& value : m_impl->m_entities.addedEntities()) {
        EntityId entityId = value.first;
        TextOverlayComponent* textOverlayComponent = std::get<0>(value.second);
        Ogre::TextAreaOverlayElement* textOverlay = static_cast<Ogre::TextAreaOverlayElement*>(
            m_impl->m_overlayManager->createOverlayElement(
                "TextArea",
                textOverlayComponent->m_name
            )
        );
        textOverlayComponent->m_overlayElement = textOverlay;
        m_impl->m_textOverlays[entityId] = textOverlay;
        m_impl->m_panel->addChild(textOverlay);
        textOverlay->setMetricsMode(Ogre::GMM_PIXELS);
    }
    for (EntityId entityId : m_impl->m_entities.removedEntities()) {
        Ogre::OverlayElement* textOverlay = m_impl->m_textOverlays[entityId];
        m_impl->m_overlayManager->destroyOverlayElement(textOverlay);
        m_impl->m_textOverlays.erase(entityId);
    }
    m_impl->m_entities.clearChanges();
    for (auto& value : m_impl->m_entities) {
        TextOverlayComponent* textOverlayComponent = std::get<0>(value.second);
        auto& properties = textOverlayComponent->m_properties;
        if (properties.hasChanges()) {
            Ogre::TextAreaOverlayElement* textOverlay = textOverlayComponent->m_overlayElement;
            std::cout << "Updating text " << textOverlay->isVisible() << std::endl;
            textOverlay->setPosition(
                properties.left,
                properties.top
            );
            textOverlay->setDimensions(
                properties.width,
                properties.height
            );
            textOverlay->setCharHeight(properties.charHeight);
            textOverlay->setColour(properties.colour);
            textOverlay->setFontName(properties.fontName);
            textOverlay->setCaption(properties.text);
            textOverlay->setHorizontalAlignment(properties.horizontalAlignment);
            textOverlay->setVerticalAlignment(properties.verticalAlignment);
            // Untouch
            properties.untouch();
        }
    }
}


