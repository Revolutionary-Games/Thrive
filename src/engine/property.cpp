#include "engine/property.h"

#include "engine/component.h"

using namespace thrive;

PropertyBase::PropertyBase(
    Component& owner,
    std::string name
) : m_name(name),
    m_owner(owner)
{
    owner.registerProperty(*this);
}


PropertyBase::~PropertyBase() {}


std::string
PropertyBase::name() const {
    return m_name;
}


Component&
PropertyBase::owner() const {
    return m_owner;
}
