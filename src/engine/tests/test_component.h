#pragma once

#include "engine/component.h"

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
        static std::string string = "TestComponent" + std::to_string(ID);
        return string;
    }

    const std::string&
    typeName() const override {
        return TYPE_NAME();
    };

};
