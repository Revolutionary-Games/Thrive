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

/**
* @brief A single touchable value
*
* @tparam T
*   The type of the underlying value
*/
template<typename T>
class TouchableValue : public Touchable {

public:

    /**
    * @brief Constructor
    *
    * @param value
    *   The initial value
    */
    TouchableValue(
        const T& value = T()
    ) : m_value(value)
    {
    }

    /**
    * @brief Assignment operator
    *
    * Also calls Touchable::touch()
    *
    * @param value
    *   The new value
    *
    * @return 
    */
    TouchableValue&
    operator =(
        const T& value
    ) {
        m_value = value;
        this->touch();
        return *this;
    }

    /**
    * @brief Implicit cast operator
    *
    * @return The underlying value
    */
    operator T() const {
        return this->get();
    }

    /**
    * @brief Explicit "cast"
    *
    * @return The underlying value
    */
    T
    get() const {
        return m_value;
    }

private:
    
    T m_value;
};

}
