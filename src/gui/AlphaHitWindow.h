#pragma once

#include <CEGUI/CEGUI.h>
#include "cegui_types.h"


class AlphaHitWindow : public CEGUI::PushButton
{
public:
    //! Window factory name.
    static const CEGUI::String WidgetTypeName;

    //! Constructor
    AlphaHitWindow(const CEGUI::String& type, const CEGUI::String& name);
    //! Destructor
    ~AlphaHitWindow();

    // overridden from Window base class
    bool
    isHit(
        const CEGUIVector2& position,
        const bool allow_disabled = false
    ) const override;
    
protected:
    //! handler to copy rendered data to a memory buffer
    bool renderingEndedHandler(const CEGUI::EventArgs& args);
    // overridden from Window base class
    bool testClassName_impl(const CEGUI::String& class_name) const;

    //! Pointer to buffer holding the render data
    CEGUI::uint32* d_hitTestBuffer;
    //! Size of the hit test buffer (i.e. its capacity)
    size_t d_hitBufferCapacity;
    //! Dimensions in pixels of the data in the hit test buffer
    CEGUI::Sizef d_hitBufferSize;
    //! whether data in hit test buffer is inverted.
    bool d_hitBufferInverted;
};
