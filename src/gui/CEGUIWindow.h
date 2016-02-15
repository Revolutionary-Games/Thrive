#pragma once

#include <CEGUI/CEGUI.h>
#include <luabind/object.hpp>
#include <OgreVector2.h>
#include <OISKeyboard.h>


namespace luabind {
class scope;
}

namespace thrive {

class StandardItemWrapper;

class CEGUIWindow {

public:

    /**
    * @brief Constructor
    *
    * @param layoutName
    *  The name of the layout file to load the window from
    **/
    CEGUIWindow(
        std::string layoutName
    );

    /**
    * @brief Constructor
    *
    * @param type
    *  The type of CEGUI window (from .scheme) to create internally
    *
    * @param name
    *  name of the window
    **/
    CEGUIWindow(
        std::string type,
        std::string name
    );

    /**
    * @brief Destructor
    **/
    virtual
    ~CEGUIWindow();

    static CEGUIWindow
    getRootWindow();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CEGUIWindow::CEGUIWindow(string)
    * - CEGUIWindow::isNull
    * - CEGUIWindow::getText
    * - CEGUIWindow::setText
    * - CEGUIWindow::appendText
    * - CEGUIWindow::getParent
    * - CEGUIWindow::getChild
    * - CEGUIWindow::addChild
    * - CEGUIWindow::removeChild
    * - CEGUIWindow::registerEventHandler
    * - CEGUIWindow::registerKeyEventHandler
    * - CEGUIWindow::enable
    * - CEGUIWindow::disable
    * - CEGUIWindow::setFocus
    * - CEGUIWindow::show
    * - CEGUIWindow::hide
    * - CEGUIWindow::moveToFront
    * - CEGUIWindow::moveToBack
    * - CEGUIWindow::moveInFront
    * - CEGUIWindow::moveBehind
    * - CEGUIWindow::setPosition
    * - CEGUIWindow::getName
    *
    * - CEGUIWindow::playAnimation
    *
    * Depending on version. Newer CEGUI versions:
    * - CEGUIWindow::listWidgetAddItem
    * - CEGUIWindow::listWidgetResetList
    * - CEGUIWindow::listWidgetGetFirstSelectedItemText
    * - CEGUIWindow::listWidgetGetFirstSelectedID
    *
    * Older versions:
    * - CEGUIWindow::listboxAddItem
    * - CEGUIWindow::listboxResetList
    * - CEGUIWindow::listboxHandleUpdatedItemData
    *
    * - CEGUIWindow::itemListboxAddItem
    * - CEGUIWindow::itemListboxResetList
    * - CEGUIWindow::itemListboxHandleUpdatedItemData
    * - CEGUIWindow::itemListboxGetLastSelectedItem
    *
    *
    *
    *
    * - CEGUIWindow::scrollingpaneGetVerticalPosition
    * - CEGUIWindow::scrollingpaneSetVerticalPosition
    *
    * - CEGUIWindow::progressbarSetProgress
    *
    * - CEGUIWindow.getWindowUnderMouse
    * - CEGUIWindow.setGuiMoveMode
    *
    * @return
    */
    static luabind::scope
    luaBindings();

    static void
    setGuiMoveMode(
        bool value
    );

    static CEGUIWindow
    getWindowUnderMouse();


    /**
    * @brief Returns whether the underlying CEGUI::Window is a nullptr
    *
    * @return isNull
    */
    bool
    isNull() const;

    /**
    * @brief Creates a new windows and adds it to the underlying CEGUI window
    *
    * @param layoutName
    *  The name of the layout to use (filename without fileending)
    *
    * @return
    *  The newly created window
    */
    CEGUIWindow
    createChildWindow(
        std::string layoutName
    );

    /**
    * @brief Adds a child to the window
    *
    * @param window
    *  Window to add
    */
    void
    addChild(
        CEGUIWindow* window
    );

    /**
    * @brief Removes a child from the window
    *
    * @param window
    *  Window with the same underlying id as the window to remove
    */
    void
    removeChild(
        CEGUIWindow* window
    );

    /**
    * @brief Destroys the underlying CEGUI Window
    */
    void
    destroy() const;

    /**
    * @brief Get the underlying cegui windows text if it has any
    *
    * @return text
    *  The requested text or empty string if none exist
    */
    std::string
    getText() const;

    /**
    * @brief Sets the underlying cegui windows text
    *
    * @param text
    *  The value to set the text to
    */
    void
    setText(
        const std::string& text
    );

    /**
    * @brief Appends to the underlying cegui windows text
    *
    * @param text
    *  The text to append
    */
    void
    appendText(
        const std::string& text
    );

    /**
    * @brief Sets the underlying windows image property
    *
    * @param text
    *  The image to use
    */
    void
    setImage(
        const std::string& image
    );

    /**
    * @brief Sets the underlying windows property
    *
    * @param text
    *  The parameter to set
    * @param text
    *  The property to be set
    */
    void
    setProperty(
        const std::string& parameter,
        const std::string& property
    );

    // Listbox has been removed from cegui.
    // For basic use use ListWidget instead
    // For more advanced view items use ListView
    // In .layout files ListWidget is called Thrive/ListView

    /**
       @brief Adds a StandardItem to the window if it is a ListWidget otherwise throws bad_cast exception
    */
    void
    listWidgetAddStandardItem(
        StandardItemWrapper* item
    );

    /**
       @brief Creates and adds a StandardItem to the window if it is a ListWidget otherwise throws bad_cast exception
    */
    void
    listWidgetAddItem(
        const std::string &text,
        int id
    );

    /**
     * @brief Adds a string to the window if it is a ListWidget otherwise throws bad_cast exception
     */
    void
    listWidgetAddTextItem(
        const std::string &text
    );

    /**
       @brief Updates an item with the new text
    */
    void
    listWidgetUpdateItem(
        StandardItemWrapper* item,
        const std::string &text
    );

    /**
     * @brief Clears the list if the window is a ListWidget otherwise throws bad_cast exception
     */
    void
    listWidgetResetList();

    std::string
    listWidgetGetFirstSelectedItemText();

    int
    listWidgetGetFirstSelectedID();




    /**
    * @brief Sets the progress of the progressbar
    *
    * @param progress
    *  Float between 0.0 and 1.0
    */
    void
    progressbarSetProgress(
       float progress
   );

    /**
    * @brief Gets the vertical scroll position
    *
    * @return
    */
    float
    scrollingpaneGetVerticalPosition();

    /**
    * @brief Sets the vertical scroll position
    *
    * @param position
    *   float between 0.0 - 1.0
    */
    void
    scrollingpaneSetVerticalPosition(
         float position
     );

    /**
    * @brief Adds an icon vertically to the scrollable pane
    *
    * @param Window to add
    */
    void
    scrollingpaneAddIcon(
        CEGUIWindow* icon
    );

    /**
    * @brief Gets the underlying cegui windows parent, wrapped as a CEGUIWindow*
    *
    * @return window
    */
    CEGUIWindow
    getParent() const;

    /**
    * @brief Gets one of the underlying cegui windows children by name, wrapped as a CEGUIWindow*
    *
    * @param name
    *  name of the child to acquire
    *
    * @return window
    */
    CEGUIWindow
    getChild(
        const std::string& name
    ) const;

    /**
    * @brief Registers an callback for a given generic CEGUI event
    *
    * @param eventName
    *  name of the event to subscribe to
    *
    * @param callback
    *  callback to use when event fires
    */
    void
    registerEventHandler(
        const std::string& eventName,
        CEGUI::Event::Subscriber callback
    ) const;

    /// Same as above but for lua callbacks
    void
    registerEventHandler(
        const std::string& eventName,
        const luabind::object& callback
    ) const ;

    /**
    * @brief Registers an callback for a given keypress CEGUI event
    *
    * @param callback
    *  callback to use when key is pressed
    */
    void
    registerKeyEventHandler(
        CEGUI::Event::Subscriber callback
    ) const;

    /// Same as above but for lua callbacks
    void
    registerKeyEventHandler(
        const luabind::object& callback
    ) const ;

    /**
    * @brief Enables the window, allowing interaction
    **/
    void
    enable();

    /**
    * @brief Disables interaction with the window
    **/
    void
    disable();

    /**
    * @brief Sets focus to the underlying cegui window
    **/
    void
    setFocus();

    /**
    * @brief Makes the window visible
    **/
    void
    show();

    /**
    * @brief Hides the window
    **/
    void
    hide();

    /**
    * @brief Moves the window in front of all other windows
    **/
    void
    moveToFront();

    /**
    * @brief Moves the window behind all other windows
    **/
    void
    moveToBack();

    /**
    * @brief Moves the window in front of target window
    *
    * @param target
    *  The window to move in front of
    **/
    void
    moveInFront(
        const CEGUIWindow& target
    );

    /**
    * @brief Moves the window behind target window
    *
    * @param target
    *  The window to move behind
    **/
    void
    moveBehind(
        const CEGUIWindow& target
    );

    /**
    * @brief Sets the windows position
    *
    * The positional system uses Falagard coordinate system.
    * The position is offset from one of the corners and edges of this Element's parent element (depending on alignments)
    *
    * @param x
    *
    * @param x
    **/
    void
    setPositionAbs(
        float x,
        float y
    );
    void
    setPositionRel(
        float x,
        float y
    );

    /**
    * @brief Sets the windows size
    *
    * The positional system uses Falagard coordinate system.
    *
    * @param width
    *
    * @param height
    **/
    void
    setSizeAbs(
        float width,
        float height
    );
    void
    setSizeRel(
        float width,
        float height
    );

    /**
    * @brief Returns the windows internal name
    *
    **/
    std::string
    getName();

    /**
    * @brief Plays an animation by name
    *
    * @param name
    *  The name of the animation to play
    **/
    void
    playAnimation(
      std::string name
    );


private:

    friend class GameState;

    //Private constructor. New window is true if this is the first time a CEGUIWindow is created with the window pointer (for event subscribing)
    CEGUIWindow(CEGUI::Window* window, bool newWindow = true);

protected:

    CEGUI::Window* m_window = nullptr;

};

}
