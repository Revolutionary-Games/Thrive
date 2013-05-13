#pragma once

#include "engine/component.h"

#include <boost/lexical_cast.hpp>

template<int ID>
class TestComponent : public thrive::Component {

public:

    static TypeId
    TYPE_ID() {
        static TypeId id = ID + 10000;
        return id;
    }

    TypeId
    typeId() const override {
        return TYPE_ID();
    };

    static const std::string&
    TYPE_NAME() {
        static std::string string = "TestComponent" + boost::lexical_cast<std::string>(ID);
        return string;
    }

    const std::string&
    typeName() const override {
        return TYPE_NAME();
    };

};
