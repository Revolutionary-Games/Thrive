#pragma once

#include "engine/component.h"
#include "engine/property.h"

template<int ID>
class TestComponent : public thrive::Component {

public:

    static const TypeId TYPE_ID = ID + 10000;

    TestComponent() 
      : thrive::Component(),
        p_bool(*this, "boolean"),
        p_double(*this, "double"),
        p_int(*this, "integer"),
        p_text(*this, "text")
    {
    }

    TypeId
    typeId() const override {
        return TYPE_ID;
    };

    const std::string&
    typeString() const override {
        static std::string string = "TestComponent" + std::to_string(ID);
        return string;
    };

    thrive::Property<bool>
    p_bool;

    thrive::Property<double>
    p_double;

    thrive::Property<int>
    p_int;

    thrive::Property<const char*>
    p_text;

};


