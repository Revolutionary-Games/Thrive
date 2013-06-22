#pragma once

namespace luabind {
    class scope;
}

namespace thrive {

/**
* @brief Helper class for keeping track of changing data
*
* Properties of components should be derived from Touchable so that the system
* that handles the component can quickly check for any changes.
*
* @note
*   A Touchable starts out with <tt> Touchable::hasChanges() == true </tt>
*/
class Touchable {

public:


    /**
    * @brief Lua bindings
    *
    * Exposes:
    * - Touchable::hasChanges()
    * - Touchable::touch()
    * - Touchable::untouch()
    *
    * @return 
    */
    static luabind::scope
    luaBindings();

    /**
    * @brief Whether this Touchable has unapplied changes
    */
    bool
    hasChanges() const;

    /**
    * @brief Marks the Touchable as changed
    */
    void
    touch();

    /**
    * @brief Marks all changes as applied
    */
    void
    untouch();

private:

    bool m_hasChanges = true;
};

}
