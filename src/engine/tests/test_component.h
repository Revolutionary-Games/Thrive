#pragma once

#include "engine/component.h"
#include "engine/property.h"

class TestComponent : public thrive::Component {
    COMPONENT(TestComponent)

public:

    TestComponent() 
      : thrive::Component(),
        p_bool(*this, "boolean"),
        p_double(*this, "double"),
        p_int(*this, "integer"),
        p_text(*this, "text")
    {
    }

    thrive::Property<bool>
    p_bool;

    thrive::Property<double>
    p_double;

    thrive::Property<int>
    p_int;

    thrive::Property<const char*>
    p_text;

};


