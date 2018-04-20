#pragma once

#include "engine/component.h"
#include "engine/serialization.h"
#include "engine/typedefs.h"

#include <boost/lexical_cast.hpp>

template<int ID> class TestComponent : public thrive::Component {

public:
    static const thrive::ComponentTypeId TYPE_ID = ID + 10000;

    thrive::ComponentTypeId
        typeId() const override
    {
        return TYPE_ID;
    };

    static const std::string&
        TYPE_NAME()
    {
        static std::string string =
            "TestComponent" + boost::lexical_cast<std::string>(ID);
        return string;
    }

    std::string
        typeName() const override
    {
        return TYPE_NAME();
    };

    void
        load(const thrive::StorageContainer& storage) override
    {
        Component::load(storage);
    }

    thrive::StorageContainer
        storage() const override
    {
        return Component::storage();
    }
};
