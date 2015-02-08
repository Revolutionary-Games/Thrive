#include "gui/CEGUIWindow.h"

#include "scripting/luabind.h"
#include "game.h"
#include "engine/engine.h"
#include "ogre/mouse.h"
#include <OgreVector3.h>
#include <luabind/object.hpp>
#include <functional>

using namespace thrive;

static int SCROLLABLE_PANE_VERTICAL_PADDING = 5;

//Static variables used for moving gui
static bool static_guiMode = false;
static CEGUI::Window* static_activeMoveWindow = nullptr;
static CEGUI::Event::Connection static_movingEvent;
static CEGUI::Vector2f static_mousePosInWindow;


static bool onWindowMove(
    const CEGUI::EventArgs& args
) {
    using namespace CEGUI;

    CEGUI::Window* parent = static_activeMoveWindow->getParent();
    CEGUI::Rectf parentRect = parent->getOuterRectClipper();

    const MouseEventArgs mouseEventArgs = static_cast<const MouseEventArgs&>(args);
    auto localMousePos = CoordConverter::screenToWindow(*static_activeMoveWindow, mouseEventArgs.position);
    UVector2 mouseMoveOffset(cegui_reldim(localMousePos.d_x/parentRect.getWidth()), cegui_reldim(localMousePos.d_y/parentRect.getHeight()));
    UVector2 mouseOffsetInWindow(cegui_reldim(static_mousePosInWindow.d_x/parentRect.getWidth()), cegui_reldim(static_mousePosInWindow.d_y/parentRect.getHeight()));
    static_activeMoveWindow->setPosition(static_activeMoveWindow->getPosition() + mouseMoveOffset - mouseOffsetInWindow);
    return true;
}

//Event handlers
static bool
handleWindowMove(
    const CEGUI::EventArgs& args
) {
    using namespace CEGUI;
    if (static_guiMode) {
        const MouseEventArgs mouseEventArgs = static_cast<const MouseEventArgs&>(args);
        auto* window = static_cast<const CEGUI::WindowEventArgs&>(args).window;
        if(mouseEventArgs.button == CEGUI::RightButton) {
            if (static_activeMoveWindow) {
                auto newPos = static_activeMoveWindow->getPosition();
                std::cout << "New position: {{" << newPos.d_x.d_scale << ", " << newPos.d_x.d_offset << "}, {" << newPos.d_y.d_scale << ", " << newPos.d_y.d_offset << "}}" << std::endl;
                static_activeMoveWindow = nullptr;
                static_movingEvent->disconnect();
            }
            else {
                CEGUI::Window* parent = window->getParent();
                if (parent) {
                    static_activeMoveWindow = window;
                    static_movingEvent = static_activeMoveWindow->subscribeEvent(CEGUI::Window::EventMouseMove, Event::Subscriber(onWindowMove));
                    static_mousePosInWindow = CoordConverter::screenToWindow(*static_activeMoveWindow, CEGUI::System::getSingleton().getDefaultGUIContext().getMouseCursor().getPosition());
                }
                else {
                    std::cout << "This CEGUI window can't be moved. Are you perhaps trying to move the background or if the window you are trying to move has the clickthrough property, then remove it while adjusting gui." << std::endl;
                }
            }
        }
        return true;
    }
    return false;
}


//Static
void
CEGUIWindow::setGuiMoveMode(
    bool value
) {
    static_guiMode = value;
}

//Static
CEGUIWindow
CEGUIWindow::getWindowUnderMouse()
{
    return CEGUIWindow(CEGUI::System::getSingleton().getDefaultGUIContext().getWindowContainingMouse());
}

//Static
CEGUIWindow
CEGUIWindow::getRootWindow()
{
    return CEGUIWindow(CEGUI::System::getSingleton().getDefaultGUIContext().getRootWindow(), false);
}


CEGUIWindow::CEGUIWindow(
    CEGUI::Window* window,
    bool newWindow
) : m_window(window)
{
    if (window && newWindow ) {
        m_window->subscribeEvent("MouseClick", handleWindowMove);
    }
}

static void
_subscribeRecursive(
    CEGUI::Window* window
) {
    unsigned int index = 0;
    while (index < window->getChildCount())
    {
       CEGUI::Window* child = window->getChildAtIdx(index);
       _subscribeRecursive(child);
       ++index;
    }
    window->subscribeEvent("MouseClick", handleWindowMove);
}

CEGUIWindow::CEGUIWindow(
    std::string layoutName
){
    m_window = CEGUI::WindowManager::getSingleton().loadLayoutFromFile(layoutName + ".layout");
    _subscribeRecursive(m_window);
}

CEGUIWindow::CEGUIWindow(
    std::string type,
    std::string name
){
    m_window = CEGUI::WindowManager::getSingleton().createWindow(type, name);
    //Only used for when the gui movement mode is activated                      );
    m_window->subscribeEvent("MouseClick", handleWindowMove);
}


CEGUIWindow::~CEGUIWindow()
{
}

luabind::scope
CEGUIWindow::luaBindings() {
    using namespace luabind;
    return class_<CEGUIWindow>("CEGUIWindow")
        //.scope
        //[
        //    def("getRootWindow", &CEGUIWindow::getRootWindow) //Better to use gameState::rootGUIWindow
        //]
        .def(constructor<std::string>())
        .def(constructor<std::string, std::string>())
        .def("isNull", &CEGUIWindow::isNull)
        .def("getText", &CEGUIWindow::getText)
        .def("setText", &CEGUIWindow::setText)
        .def("appendText", &CEGUIWindow::appendText)
        .def("setImage", &CEGUIWindow::setImage)
        .def("getParent", &CEGUIWindow::getParent)
        .def("getChild", &CEGUIWindow::getChild)
        .def("addChild", &CEGUIWindow::addChild)
        .def("removeChild", &CEGUIWindow::removeChild)
        .def("registerEventHandler",
             static_cast<void (CEGUIWindow::*)(const std::string&, const luabind::object&) const>(&CEGUIWindow::registerEventHandler)
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
        .def("setPositionAbs", &CEGUIWindow::setPositionAbs)
        .def("setPositionRel", &CEGUIWindow::setPositionRel)
        .def("setSizeAbs", &CEGUIWindow::setSizeAbs)
        .def("setSizeRel", &CEGUIWindow::setSizeRel)
        .def("getName", &CEGUIWindow::getName)
        .def("playAnimation", &CEGUIWindow::playAnimation)
        .def("listboxAddItem", &CEGUIWindow::listboxAddItem)
        .def("listboxResetList", &CEGUIWindow::listboxResetList)
        .def("listboxHandleUpdatedItemData", &CEGUIWindow::listboxHandleUpdatedItemData)
        .def("itemListboxAddItem", &CEGUIWindow::itemListboxAddItem)
        .def("itemListboxResetList", &CEGUIWindow::itemListboxResetList)
        .def("itemListboxHandleUpdatedItemData", &CEGUIWindow::itemListboxHandleUpdatedItemData)
        .def("itemListboxGetLastSelectedItem", &CEGUIWindow::itemListboxGetLastSelectedItem)
        .def("progressbarSetProgress", &CEGUIWindow::progressbarSetProgress)
        .def("scrollingpaneAddIcon", &CEGUIWindow::scrollingpaneAddIcon)
        .def("scrollingpaneGetVerticalPosition", &CEGUIWindow::scrollingpaneGetVerticalPosition)
        .def("scrollingpaneSetVerticalPosition", &CEGUIWindow::scrollingpaneSetVerticalPosition)
        .def("registerKeyEventHandler",
             static_cast<void (CEGUIWindow::*)(const luabind::object&) const>(&CEGUIWindow::registerKeyEventHandler)
         )
        .scope
        [
            def("setGuiMoveMode", &CEGUIWindow::setGuiMoveMode),
            def("getWindowUnderMouse", &CEGUIWindow::getWindowUnderMouse)
        ]
    ;
}

bool
CEGUIWindow::isNull() const {
    return m_window == nullptr;
}

CEGUIWindow
CEGUIWindow::createChildWindow(
    std::string layoutName
){
    CEGUI::Window* newWindow = CEGUI::WindowManager::getSingleton().loadLayoutFromFile(layoutName + ".layout");
    m_window->addChild(newWindow);
    return CEGUIWindow(newWindow, true);
}



void
CEGUIWindow::addChild(CEGUIWindow* window){
    m_window->addChild(window->m_window);
}

void
CEGUIWindow::removeChild(CEGUIWindow* window){
    m_window->removeChild(window->m_window->getID());
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
CEGUIWindow::setImage(
    const std::string& image
) {
    m_window->setProperty("Image", image);
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

void
CEGUIWindow::itemListboxAddItem(
    CEGUIWindow* item
) {
    dynamic_cast<CEGUI::ItemListBase*>(m_window)->addItem(dynamic_cast<CEGUI::ItemEntry*>(item->m_window));
}

void
CEGUIWindow::itemListboxResetList(){
    dynamic_cast<CEGUI::ItemListBase*>(m_window)->resetList();
}

void
CEGUIWindow::itemListboxHandleUpdatedItemData(){
    dynamic_cast<CEGUI::ItemListBase*>(m_window)->handleUpdatedItemData();
}

CEGUIWindow*
CEGUIWindow::itemListboxGetLastSelectedItem(){
    return new CEGUIWindow(dynamic_cast<CEGUI::ItemListbox*>(m_window)-> getLastSelectedItem(), false);
}



void
CEGUIWindow::progressbarSetProgress(float progress){
    dynamic_cast<CEGUI::ProgressBar*>(m_window)->setProgress(progress);
}

float
CEGUIWindow::scrollingpaneGetVerticalPosition()
{
    return dynamic_cast<CEGUI::ScrollablePane*>(m_window)->getVerticalScrollPosition();
}

void
CEGUIWindow::scrollingpaneSetVerticalPosition(float position)
{
    dynamic_cast<CEGUI::ScrollablePane*>(m_window)->setVerticalScrollPosition(position);
}

void
CEGUIWindow::scrollingpaneAddIcon(CEGUIWindow* icon)
{
    icon->setPositionAbs(0, dynamic_cast<CEGUI::ScrollablePane*>(m_window)->getContentPaneArea().getHeight()+SCROLLABLE_PANE_VERTICAL_PADDING);
    m_window->addChild(icon->m_window);
}


CEGUIWindow
CEGUIWindow::getParent() const {
    return CEGUIWindow(m_window->getParent(), false);
}


CEGUIWindow
CEGUIWindow::getChild(
    const std::string& name
) const  {
    return CEGUIWindow(m_window->getChild(name), false);
}


void
CEGUIWindow::registerEventHandler(
    const std::string& eventName,
    CEGUI::Event::Subscriber callback
) const {
    m_window->subscribeEvent(eventName, callback);
}


void
CEGUIWindow::registerEventHandler(
    const std::string& eventName,
    const luabind::object& callback
) const {
    // Lambda must return something to avoid an template error.
    auto callbackLambda = [callback](const CEGUI::EventArgs& args) -> int
        {
            luabind::call_function<void>(callback, CEGUIWindow(static_cast<const CEGUI::WindowEventArgs&>(args).window, false));
            return 0;
        };
    m_window->subscribeEvent(eventName, callbackLambda);
}

void
CEGUIWindow::registerKeyEventHandler(
    CEGUI::Event::Subscriber callback
) const {
    m_window->subscribeEvent("CharacterKey", callback);
}

void
CEGUIWindow::registerKeyEventHandler(
    const luabind::object& callback
) const {
    auto callbackLambda = [callback](const CEGUI::EventArgs& args) -> int
        {
            luabind::call_function<void>(callback, CEGUIWindow(static_cast<const CEGUI::WindowEventArgs&>(args).window, false), static_cast<int>(static_cast<const CEGUI::KeyEventArgs&>(args).scancode) );
            return 0;
        };
    m_window->subscribeEvent(CEGUI::PushButton::EventKeyDown, callbackLambda);
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
CEGUIWindow::setPositionAbs(
    float x,
    float y
){
    m_window->setPosition(CEGUI::Vector2<CEGUI::UDim>(CEGUI::UDim(0,x), CEGUI::UDim(0,y)));
}

void
CEGUIWindow::setPositionRel(
    float x,
    float y
){
    m_window->setPosition(CEGUI::Vector2<CEGUI::UDim>(CEGUI::UDim(x, 0), CEGUI::UDim(y, 0)));
}

void
CEGUIWindow::setSizeAbs(
    float width,
    float height
){
    m_window->setSize( CEGUI::Size<CEGUI::UDim>(CEGUI::UDim(0, width), CEGUI::UDim(0,height))   );
}
void
CEGUIWindow::setSizeRel(
    float width,
    float height
){
    m_window->setSize( CEGUI::Size<CEGUI::UDim>(CEGUI::UDim(width, 0), CEGUI::UDim(height, 0))   );
}

std::string
CEGUIWindow::getName() {
    return std::string(m_window->getName().c_str());
}

void
CEGUIWindow::playAnimation(
  std::string name
) {
    auto anim = CEGUI::AnimationManager::getSingleton().instantiateAnimation(name);
    anim->setTargetWindow(m_window);
    anim->start();
}
