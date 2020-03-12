#pragma once

#include "engine/component_types.h"

#include "Entities/Component.h"
#include "Entities/System.h"



namespace thrive {



class DamageOnTouchComponent : public Leviathan::Component {
public:
    DamageOnTouchComponent();

    float damage = 0.0f;
    bool deletes = false;

    REFERENCE_HANDLE_UNCOUNTED_TYPE(DamageOnTouchComponent);

    static constexpr auto TYPE =
        componentTypeConvert(THRIVE_COMPONENT::DAMAGETOUCH);

    void
        setDamage(double damage);

    double
        getDamage();
    void
        setDeletes(bool deletes);

    bool
        getDeletes();
};


} // namespace thrive
