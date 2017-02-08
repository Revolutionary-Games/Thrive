#pragma once

#include <CEGUI/widgets/PushButton.h>
#include "cegui_types.h"

#include <memory>

class TextureAlphaCheckArea;

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

    //! Handles finding a texture and the position in it for an image name
    static std::unique_ptr<TextureAlphaCheckArea>
        getTextureFromCEGUIImageName(const CEGUI::String& name);
    
protected:
    // overridden from Window base class
    bool testClassName_impl(const CEGUI::String& class_name) const;


    //! Once the texture and position in it has been determine it is stored here
    //!
    //! This is mutable because isHit has to be a const method
    mutable std::unique_ptr<TextureAlphaCheckArea> m_hitTestTexture;
};
