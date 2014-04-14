#include "gui/CEGUIWindow.h"

#include "scripting/luabind.h"
#include <luabind/object.hpp>

using namespace thrive;


//Static
CEGUIWindow
CEGUIWindow::getRootWindow()
{
    return CEGUIWindow(CEGUI::System::getSingleton().getDefaultGUIContext().getRootWindow());
}


CEGUIWindow::CEGUIWindow(
    CEGUI::Window* window
) : m_window(window)
{
}

CEGUIWindow::CEGUIWindow(
    std::string layoutName
){
    m_window = CEGUI::WindowManager::getSingleton().loadLayoutFromFile(layoutName + ".layout");
}

CEGUIWindow::~CEGUIWindow()
{
}

luabind::scope
CEGUIWindow::luaBindings() {
    using namespace luabind;
    return class_<CEGUIWindow>("CEGUIWindow")
        .scope
        [
            def("getRootWindow", &CEGUIWindow::getRootWindow)
        ]
        .def(constructor<std::string>())
        .def("getText", &CEGUIWindow::getText)
        .def("setText", &CEGUIWindow::setText)
        .def("listboxAddItem", &CEGUIWindow::listboxAddItem)
        .def("listboxResetList", &CEGUIWindow::listboxResetList)
        .def("listboxHandleUpdatedItemData", &CEGUIWindow::listboxHandleUpdatedItemData)
        .def("appendText", &CEGUIWindow::appendText)
        .def("getParent", &CEGUIWindow::getParent)
        .def("getChild", &CEGUIWindow::getChild)
        .def("addChild", &CEGUIWindow::addChild)
        .def("removeChild", &CEGUIWindow::removeChild)
        .def("registerEventHandler",
             static_cast<void (CEGUIWindow::*)(const std::string&, const luabind::object&) const>(&CEGUIWindow::RegisterEventHandler)
         )
        .def("enable", &CEGUIWindow::enable)
        .def("disable", &CEGUIWindow::disable)
        .def("setFocus", &CEGUIWindow::setFocus)
        .def("show", &CEGUIWindow::show)
        .def("hide", &CEGUIWindow::hide)
        .def("moveToFront", &CEGUIWindow::moveToFront)
        .def("moveToBack", &CEGUIWindow::moveToBack)
        .def("moveInFront", &CEGUIWindow::moveInFront)
        .def("moveBehind", &CEGUIWindow::moveBehind)
        .def("setPosition", &CEGUIWindow::setPosition)
    ;
}


CEGUIWindow
CEGUIWindow::createChildWindow(
    std::string layoutName
){
    CEGUI::Window* newWindow = CEGUI::WindowManager::getSingleton().loadLayoutFromFile(layoutName + ".layout");
    m_window->addChild(newWindow);
    return CEGUIWindow(newWindow);
}

void
CEGUIWindow::addChild(CEGUIWindow& window){
    m_window->addChild(window.m_window);
}

void
CEGUIWindow::removeChild(CEGUIWindow& window){
    m_window->removeChild(window.m_window->getID());
}


void
CEGUIWindow::destroy() const {
    m_window->destroy();
}

std::string
CEGUIWindow::getText() const {
    return std::string(m_window->getText().c_str());
}


void
CEGUIWindow::setText(
    const std::string& text
) {
    m_window->setText(text);
}


void
CEGUIWindow::appendText(
    const std::string& text
) {
    m_window->appendText(text);
}

void
CEGUIWindow::listboxAddItem(
    CEGUI::ListboxTextItem* listboxItem
) {
    dynamic_cast<CEGUI::Listbox*>(m_window)->addItem(listboxItem);
}

void
CEGUIWindow::listboxResetList(){
    dynamic_cast<CEGUI::Listbox*>(m_window)->resetList();
}

void
CEGUIWindow::listboxHandleUpdatedItemData(){
    dynamic_cast<CEGUI::Listbox*>(m_window)->handleUpdatedItemData();
}

CEGUIWindow
CEGUIWindow::getParent() const {
    return CEGUIWindow(m_window->getParent());
}


CEGUIWindow
CEGUIWindow::getChild(
    const std::string& name
) const  {
    return CEGUIWindow(m_window->getChild(name));
}


void
CEGUIWindow::RegisterEventHandler(
    const std::string& eventName,
    CEGUI::Event::Subscriber callback
) const {
    m_window->subscribeEvent(eventName, callback);
}


void
CEGUIWindow::RegisterEventHandler(
    const std::string& eventName,
    const luabind::object& callback
) const {
    // Lambda must return something to avoid an template error.
    auto callbackLambda = [callback](const CEGUI::EventArgs&) -> int
        {
            luabind::call_function<void>(callback);
            return 0;
        };
    m_window->subscribeEvent(eventName, callbackLambda);
}


void
CEGUIWindow::enable(){
    m_window->enable();
}


void
CEGUIWindow::disable(){
    m_window->disable();
}


void
CEGUIWindow::setFocus() {
    m_window->activate();
}


void
CEGUIWindow::show(){
    m_window->show();
}


void
CEGUIWindow::hide(){
    m_window->hide();
}


void
CEGUIWindow::moveToFront(){
    m_window->moveToFront();
}


void
CEGUIWindow::moveToBack(){
    m_window->moveToBack();
}


void
CEGUIWindow::moveInFront(
    const CEGUIWindow& target
){
    m_window->moveInFront(target.m_window);
}


void
CEGUIWindow::moveBehind(
    const CEGUIWindow& target
){
    m_window->moveBehind(target.m_window);
}


void
CEGUIWindow::setPosition(
    Ogre::Vector2 position
){
    m_window->setPosition(CEGUI::Vector2<CEGUI::UDim>(CEGUI::UDim(position.x, 0), CEGUI::UDim(position.y, 0)));
}
