// Thrive Game
// Copyright (C) 2013-2018  Revolutionary Games
#pragma once
// ------------------------------------ //
#include <GUI/LeviathanJavaScriptAsync.h>

namespace thrive {

class ThriveJSInterface : public Leviathan::GUI::JSAsyncCustom {
public:
    ThriveJSInterface();
    ~ThriveJSInterface();

    //! Query processing function
    bool
        ProcessQuery(Leviathan::GUI::LeviathanJavaScriptAsync* caller,
            const CefString& request,
            int64 queryid,
            bool persists,
            CefRefPtr<Callback>& callback) override;

    //! Previously made ProcessQuery is canceled
    void
        CancelQuery(Leviathan::GUI::LeviathanJavaScriptAsync* caller,
            int64 queryid) override;

    //! Called when a Gui::View is closed, should CancelQuery all matching ones
    void
        CancelAllMine(Leviathan::GUI::LeviathanJavaScriptAsync* me) override;

protected:
    //! Store queries that need to be handled async
};

class ThriveJSHandler : public CefV8Handler, public Leviathan::ThreadSafe {
public:
    ThriveJSHandler(Leviathan::GUI::CefApplication* owner);
    ~ThriveJSHandler();

    //! \brief Handles calls from javascript
    bool
        Execute(const CefString& name,
            CefRefPtr<CefV8Value> object,
            const CefV8ValueList& arguments,
            CefRefPtr<CefV8Value>& retval,
            CefString& exception) override;

    // This needs to be implemented if we need this
    // void ClearContextValues();


    IMPLEMENT_REFCOUNTING(ThriveJSHandler);

protected:
    //! Owner stored to be able to use it to bridge our requests to Gui::View
    Leviathan::GUI::CefApplication* Owner;
};

CefRefPtr<CefV8Handler>
    makeThriveJSHandler(Leviathan::GUI::CefApplication* application);

} // namespace thrive
