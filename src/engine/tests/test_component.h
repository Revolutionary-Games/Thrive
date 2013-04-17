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
        p_boundedValue(*this, "boundedValue", *this),
        p_double(*this, "double"),
        p_int(*this, "integer"),
        p_text(*this, "text")
    {
    }

    int
    getBoundedValue() const {
        return m_boundedValue;
    }

    bool
    setBoundedValue(
        int value
    ) {
        if (value < -10) {
            value = -10;
        }
        else if (value > 10) {
            value = 10;
        }
        if (value != m_boundedValue) {
            m_boundedValue = value;
            return true;
        }
        else {
            return false;
        }
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

    thrive::SimpleProperty<bool>
    p_bool;

    thrive::Property<int, TestComponent, &TestComponent::getBoundedValue, &TestComponent::setBoundedValue>
    p_boundedValue;

    thrive::SimpleProperty<double>
    p_double;

    thrive::SimpleProperty<int>
    p_int;

    thrive::SimpleProperty<const char*>
    p_text;

private:

    int m_boundedValue = 0;

};


