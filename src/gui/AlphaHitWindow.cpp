#include "gui/AlphaHitWindow.h"

//----------------------------------------------------------------------------//
const CEGUI::String AlphaHitWindow::WidgetTypeName("AlphaHitWindow");

//----------------------------------------------------------------------------//
AlphaHitWindow::AlphaHitWindow(const CEGUI::String& type, const CEGUI::String& name) :
    CEGUI::PushButton(type, name),
    d_hitTestBuffer(0),
    d_hitBufferCapacity(0)
{
    // always use this since we want to sample the pre-composed imagery when we
    // do hit testing and this requires we have texture backing.
    setUsingAutoRenderingSurface(true);

    // here we subscribe an event which will grab a copy of the rendered content
    // each time it is rendered, so we can easily sample it without needing to
    // read texture content for every mouse move / hit test.
    CEGUI::RenderingSurface* rs = getRenderingSurface();
    if (rs)
        rs->subscribeEvent(CEGUI::RenderingSurface::EventRenderQueueEnded,
            CEGUI::Event::Subscriber(&AlphaHitWindow::renderingEndedHandler, this));
}

//----------------------------------------------------------------------------//
AlphaHitWindow::~AlphaHitWindow()
{
    delete[] d_hitTestBuffer;
}

//----------------------------------------------------------------------------//
bool
AlphaHitWindow::isHit(
    const CEGUIVector2& position,
    const bool allow_disabled
) const {

    // still do the rect test, since we only want to do the detailed test
    // if absolutely neccessary.
    if (!CEGUI::PushButton::isHit(position, allow_disabled))
        return false;

    // if buffer is not allocated, just hit against area rect, so return true
    if (!d_hitTestBuffer)
        return true;

    const CEGUIVector2 wpos(CEGUI::CoordConverter::screenToWindow(*this, position));
    const size_t idx = (d_hitBufferInverted ?
    d_hitBufferSize.d_height - wpos.y :
    wpos.y) * d_hitBufferSize.d_width + wpos.x;

    return (d_hitTestBuffer[idx] >> 24) > 0;
}

//----------------------------------------------------------------------------//
bool AlphaHitWindow::renderingEndedHandler(const CEGUI::EventArgs& args)
{
    if (static_cast<const CEGUI::RenderQueueEventArgs&>(args).queueID != CEGUI::RQ_BASE)
        return false;

    // rendering surface needs to exist and needs to be texture backed
    CEGUI::RenderingSurface* const rs = getRenderingSurface();
    if (!rs || !rs->isRenderingWindow())
        return false;

    CEGUI::TextureTarget& tt =
        static_cast<CEGUI::RenderingWindow* const>(rs)->getTextureTarget();

    CEGUI::Texture& texture = tt.getTexture();
    const CEGUI::Sizef tex_sz(texture.getSize());
    const size_t reqd_capacity =
        static_cast<int>(tex_sz.d_width) * static_cast<int>(tex_sz.d_height);

    // see if we need to reallocate buffer:
    if (reqd_capacity > d_hitBufferCapacity)
    {
        delete[] d_hitTestBuffer;
        d_hitTestBuffer = 0;
        d_hitBufferCapacity = 0;
    }

    // allocate buffer to hold data if it's not already allocated
    if (!d_hitTestBuffer)
    {
        d_hitTestBuffer = new CEGUI::uint32[reqd_capacity];
        d_hitBufferCapacity = reqd_capacity;
    }

    // save details about what will be in the buffer
    d_hitBufferInverted = CEGUI::System::getSingleton().getRenderer()->isTexCoordSystemFlipped();
    d_hitBufferSize = tex_sz;

    // grab a copy of the data.
    texture.blitToMemory(d_hitTestBuffer);

    return true;
}

