#include "gui/CEGUIWindow.h"

#include "engine/engine.h"
#include "game.h"
#include "ogre/mouse.h"
#include "script_wrappers.h"

#include <OgreVector3.h>
#include <functional>

#include <CEGUI/Element.h>
#include <CEGUI/InputEvent.h>
#include <CEGUI/Image.h>
#include <CEGUI/USize.h>

#include "cegui_types.h"

using namespace thrive;

static int SCROLLABLE_PANE_VERTICAL_PADDING = 5;

//Static variables used for moving gui
static bool static_guiMode = false;
static CEGUI::Event::Connection static_movingEvent;
static CEGUIVector2 static_mousePosInWindow;

//Event handlers
static bool
handleWindowMove(
    const CEGUI::EventArgs& args
) {
    using namespace CEGUI;
    if (static_guiMode) {

        auto eventArgs = static_cast<const WindowEventArgs&>(args);

        eventArgs.window->getParent();

        // Still no idea what's going on
        // Unless this is a really awkward way of allowing windows to move under some conditions
        return true;
    }
    return false;
}

/*
static CEGUI::Window* static_activeMoveWindow = nullptr;
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
*/

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
    return CEGUIWindow(CEGUI::System::getSingleton().getDefaultGUIContext().
        getWindowContainingCursor());
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

void CEGUIWindow::luaBindings(
    sol::state &lua
){
    lua.new_usertype<CEGUIWindow>("CEGUIWindow",

        sol::constructors<sol::types<std::string>, sol::types<std::string, std::string>>(),

        "isNull", &CEGUIWindow::isNull,
        "getText", &CEGUIWindow::getText,
        "setText", &CEGUIWindow::setText,
        "appendText", &CEGUIWindow::appendText,
        "setImage", &CEGUIWindow::setImage,
        "setProperty", &CEGUIWindow::setProperty,
        "getParent", &CEGUIWindow::getParent,
        "getChild", &CEGUIWindow::getChild,
        "addChild", &CEGUIWindow::addChild,
        "removeChild", &CEGUIWindow::removeChild,
        "registerEventHandler", static_cast<void (CEGUIWindow::*)(const std::string&,
            const sol::function&) const>(&CEGUIWindow::registerEventHandler),
        "enable", &CEGUIWindow::enable,
        "disable", &CEGUIWindow::disable,
        "setFocus", &CEGUIWindow::setFocus,
        "show", &CEGUIWindow::show,
        "hide", &CEGUIWindow::hide,
        "moveToFront", &CEGUIWindow::moveToFront,
        "moveToBack", &CEGUIWindow::moveToBack,
        "moveInFront", &CEGUIWindow::moveInFront,
        "moveBehind", &CEGUIWindow::moveBehind,
        "setPositionAbs", &CEGUIWindow::setPositionAbs,
        "setPositionRel", &CEGUIWindow::setPositionRel,
        "setSizeAbs", &CEGUIWindow::setSizeAbs,
        "setSizeRel", &CEGUIWindow::setSizeRel,
        "getName", &CEGUIWindow::getName,
        "playAnimation", &CEGUIWindow::playAnimation,
        "listWidgetAddItem", &CEGUIWindow::listWidgetAddStandardItem,
        "listWidgetAddItem", &CEGUIWindow::listWidgetAddTextItem,
        "listWidgetAddItem", &CEGUIWindow::listWidgetAddItem,
        "listWidgetResetList", &CEGUIWindow::listWidgetResetList,
        "listWidgetUpdateItem", &CEGUIWindow::listWidgetUpdateItem,
        "listWidgetGetFirstSelectedID", &CEGUIWindow::listWidgetGetFirstSelectedID,
        "listWidgetGetFirstSelectedItemText", &CEGUIWindow::listWidgetGetFirstSelectedItemText,
        "progressbarSetProgress", &CEGUIWindow::progressbarSetProgress,
        "scrollingpaneAddIcon", &CEGUIWindow::scrollingpaneAddIcon,
        "scrollingpaneGetVerticalPosition", &CEGUIWindow::scrollingpaneGetVerticalPosition,
        "scrollingpaneSetVerticalPosition", &CEGUIWindow::scrollingpaneSetVerticalPosition,
        "registerKeyEventHandler", static_cast<void (CEGUIWindow::*)(const sol::function&)
        const>(&CEGUIWindow::registerKeyEventHandler),
        "setGuiMoveMode", &CEGUIWindow::setGuiMoveMode,
        "getWindowUnderMouse", &CEGUIWindow::getWindowUnderMouse
    );
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
    return m_window->getText().c_str();
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
CEGUIWindow::setProperty(
    const std::string& argument,
    const std::string& property
) {
    m_window->setProperty(property, argument);
}

void
CEGUIWindow::listWidgetAddStandardItem(
     StandardItemWrapper* item
) {

    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    auto actualItem = item->getItem();

    list->addItem(actualItem);

    item->markAttached();
}

void
CEGUIWindow::listWidgetAddItem(
    const std::string &text,
    int id
) {

    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    list->addItem(new CEGUI::StandardItem(text.c_str(), id));
}

void
CEGUIWindow::listWidgetAddTextItem(
    const std::string &text
) {

    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    list->addItem(text);
}

void
CEGUIWindow::listWidgetUpdateItem(
    StandardItemWrapper* item,
    const std::string &text
) {


    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    // Should always be this so static cast could be fine
    auto model = list->getModel();

    model->updateItemText(item->getItem(), text);
}

void
CEGUIWindow::listWidgetResetList(){

    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    list->clearList();
}

std::string
CEGUIWindow::listWidgetGetFirstSelectedItemText(){

    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    auto selected = list->getFirstSelectedItem();

    if(!selected)
        return "";

    return selected->getText().c_str();
}

int
CEGUIWindow::listWidgetGetFirstSelectedID(){

    auto list = dynamic_cast<CEGUI::ListWidget*>(m_window);

    if(!list)
        throw std::bad_cast();

    auto selected = list->getFirstSelectedItem();

    if(!selected)
        return -1;

    return selected->getId();
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
    const sol::function& callback
) const {

    // Lambda must return something to avoid an template error.
    auto callbackLambda = [callback](const CEGUI::EventArgs& args) -> bool
        {
            callback(CEGUIWindow(static_cast<const CEGUI::WindowEventArgs&>(args).window,
                    false));
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
    const sol::function& callback
) const {
    // Event doesn't exist anymore //
    auto callbackLambda = [callback](const CEGUI::EventArgs& args) -> bool
        {
            callback(CEGUIWindow(static_cast<
                    const CEGUI::WindowEventArgs&>(args).window, false),
                static_cast<int>(static_cast<const CEGUI::TextEventArgs&>(args).d_character));
            return 0;
        };

    m_window->subscribeEvent(CEGUI::Window::EventCharacterKey, callbackLambda);
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
    m_window->setPosition(CEGUI::UVector2(CEGUI::UDim(0,x), CEGUI::UDim(0,y)));
}

void
CEGUIWindow::setPositionRel(
    float x,
    float y
){
    m_window->setPosition(CEGUI::UVector2(CEGUI::UDim(x, 0), CEGUI::UDim(y, 0)));
}

void
CEGUIWindow::setSizeAbs(
    float width,
    float height
){
    m_window->setSize( CEGUI::USize(CEGUI::UDim(0, width), CEGUI::UDim(0,height))   );
}
void
CEGUIWindow::setSizeRel(
    float width,
    float height
){
    m_window->setSize( CEGUI::USize(CEGUI::UDim(width, 0), CEGUI::UDim(height, 0))   );
}

std::string
CEGUIWindow::getName() {
    return m_window->getName().c_str();
}

void
CEGUIWindow::playAnimation(
  std::string name
) {
    auto anim = CEGUI::AnimationManager::getSingleton().instantiateAnimation(name);
    anim->setTargetWindow(m_window);
    anim->start();
}
