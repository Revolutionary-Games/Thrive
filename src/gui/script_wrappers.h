#pragma once

#include <string>

namespace CEGUI {

    class StandardItem;
}

namespace sol {
class state;
}

namespace thrive {

// StandardItemWrapper
class StandardItemWrapper{
public:
    //! @brief Constructs a wrapper around CEGUI::StandardItem(text, id)
    StandardItemWrapper(
        const std::string &text,
        int id
    );

    //! @brief Destroys the CEGUI object if it hasn't been attached
    ~StandardItemWrapper();

    //! @brief Returns the underlying CEGUI object
    CEGUI::StandardItem*
    getItem();

    //! Once attached CEGUI will handle deleting so this stops the desctructor from deleting
    void markAttached();

    
private:

    bool m_attached;
    CEGUI::StandardItem* m_item;
};

class ScriptWrappers final{
public:  
    static luabind::scope
        StandardItemWrapperBindings();

};

}

