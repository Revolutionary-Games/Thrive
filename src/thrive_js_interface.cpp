// ------------------------------------ //
#include "thrive_js_interface.h"

#include "ThriveGame.h"

#include "thrive_version.h"

using namespace thrive;
// ------------------------------------ //
ThriveJSInterface::ThriveJSInterface() {}

ThriveJSInterface::~ThriveJSInterface() {}
// ------------------------------------ //
#define JS_ACCESSCHECKPTR(x, y)           \
    if(y->_VerifyJSAccess(x, callback)) { \
        return true;                      \
    }
// ------------------------------------ //
bool
    ThriveJSInterface::ProcessQuery(
        Leviathan::GUI::LeviathanJavaScriptAsync* caller,
        const CefString& request,
        int64 queryid,
        bool persists,
        CefRefPtr<Callback>& callback)
{
    // Do whatever to handle this //
    if(request == "thriveVersion") {
        // Check rights //
        JS_ACCESSCHECKPTR(
            Leviathan::GUI::VIEW_SECURITYLEVEL_ACCESS_ALL, caller);

        // Return the result //
        callback->Success(Thrive_VERSIONS);
        return true;
    }

    // Not handled //
    return false;
}

void
    ThriveJSInterface::CancelQuery(
        Leviathan::GUI::LeviathanJavaScriptAsync* caller,
        int64 queryid)
{
    // Remove the query matching caller and queryid //
}
// ------------------------------------ //
void
    ThriveJSInterface::CancelAllMine(
        Leviathan::GUI::LeviathanJavaScriptAsync* me)
{
    // Remove all stored queries matching me and any id //
}
// ------------------------------------ //


// ------------------------------------ //
// ThriveJSHandler
ThriveJSHandler::ThriveJSHandler(Leviathan::GUI::CefApplication* owner) :
    Owner(owner)
{}

ThriveJSHandler::~ThriveJSHandler() {}
// ------------------------------------ //
bool
    ThriveJSHandler::Execute(const CefString& name,
        CefRefPtr<CefV8Value> object,
        const CefV8ValueList& arguments,
        CefRefPtr<CefV8Value>& retval,
        CefString& exception)
{
    if(name == "startNewGame") {

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "startNewGame");

        Owner->SendCustomExtensionMessage(message);
        return true;

    } else if(name == "editorButtonClicked") {

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "editorButtonClicked");

        Owner->SendCustomExtensionMessage(message);
        return true;

    } else if(name == "finishEditingClicked") {

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "finishEditingClicked");

        Owner->SendCustomExtensionMessage(message);
        return true;
    } else if(name == "killPlayerCellClicked") {
        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "killPlayerCellClicked");

        Owner->SendCustomExtensionMessage(message);
        return true;
    } else if(name == "exitToMenuClicked") {
        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "exitToMenuClicked");

        Owner->SendCustomExtensionMessage(message);
        return true;
    }

    // This might be a bit expensive...
    exception = L"Unknown ThriveJSHandler function: " + name.ToWString();
    return true;
}
// ------------------------------------ //
// Factory
CefRefPtr<CefV8Handler>
    thrive::makeThriveJSHandler(Leviathan::GUI::CefApplication* application)
{
    return new ThriveJSHandler(application);
}

// ------------------------------------ //
// ThriveJSMessageHandler
bool
    ThriveJSMessageHandler::OnProcessMessageReceived(
        CefRefPtr<CefBrowser> browser,
        CefProcessId source_process,
        CefRefPtr<CefProcessMessage> message)
{
    const auto args = message->GetArgumentList();
    const auto& customType = args->GetString(0);

    if(customType == "startNewGame") {

        LOG_INFO("Got start game message from GUI process");
        ThriveGame::Get()->startNewGame();
        return true;

    } else if(customType == "editorButtonClicked") {

        ThriveGame::Get()->editorButtonClicked();
        return true;

    } else if(customType == "finishEditingClicked") {

        ThriveGame::Get()->finishEditingClicked();
        return true;
    } else if(customType == "killPlayerCellClicked") {

        ThriveGame::Get()->killPlayerCellClicked();
        return true;
    } else if(customType == "exitToMenuClicked") {

        ThriveGame::Get()->exitToMenuClicked();
        return true;
    }
    // Not ours
    return false;
}
