#pragma once

#include <CEGUI/CEGUI.h>
#include <luabind/object.hpp>
#include <OgreVector2.h>


namespace luabind {
class scope;
}

namespace thrive {

class CEGUIWindow {

public:

    /**
    * Constructor
    **/
    CEGUIWindow(
        std::string layoutName
    );

    /**
    * Destructor
    **/
    virtual
    ~CEGUIWindow();

    static CEGUIWindow
    getRootWindow();

    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - CEGUIWindow::getRootWindow (static)
    * - CEGUIWindow::CEGUIWindow(string)
    * - CEGUIWindow::getText
    * - CEGUIWindow::setText
    * - CEGUIWindow::appendText
    * - CEGUIWindow::getParent
    * - CEGUIWindow::getChild
    * - CEGUIWindow::addChild
    * - CEGUIWindow::removeChild
    * - CEGUIWindow::registerEventHandler
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
    *
    * - CEGUIWindow::listboxAddItem
    * - CEGUIWindow::listboxResetList
    * - CEGUIWindow::listboxHandleUpdatedItemData
    *
    * - CEGUIWindow::progressbarSetProgress
    *
    * @return
    */
    static luabind::scope
    luaBindings();

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
        CEGUIWindow& window
    );

    /**
    * @brief Removes a child from the window
    *
    * @param window
    *  Window with the same underlying id as the window to remove
    */
    void
    removeChild(
        CEGUIWindow& window
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
    * @brief Adds a string to the window if it is a listbox otherwise throws bad_cast exception
    *
    * @param text
    *  The text to add
    */
    void
    listboxAddItem(
        CEGUI::ListboxTextItem* listboxItem
    );

    /**
    * @brief Clears the list if the window is a listbox otherwise throws bad_cast exception
    */
    void
    listboxResetList();

    /**
    * @brief Updates the rendering of the listbox
    */
    void
    listboxHandleUpdatedItemData();

    /**
    * @brief Sets the progress of the progressbar
    *
    * @param progress
    *  Float between 0.0 and 1.0
    */
    void
    progressbarSetProgress(float progress);

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
    * @brief Gets one of the underlying cegui windows children by name, wrapped as a CEGUIWindow*
    *
    * @param eventName
    *  name of the event to subscribe to
    *
    * @param callback
    *  callback to use when event fires
    */
    void
    RegisterEventHandler(
        const std::string& eventName,
        CEGUI::Event::Subscriber callback
    ) const;

    // Same as above but for lua callbacks
    void
    RegisterEventHandler(
        const std::string& eventName,
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
    * @param position
    *  The new position to use
    **/
    void
    setPosition(
        Ogre::Vector2 position
    );



private:

    friend class GameState;

    //Private constructor
    CEGUIWindow(CEGUI::Window* window);

    CEGUI::Window* m_window = nullptr;

};

}
