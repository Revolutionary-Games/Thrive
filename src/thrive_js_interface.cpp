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

    } else if(name == "freebuildEditorButtonClicked") {

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "freebuildEditorButtonClicked");

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
    } else if(name == "connectToServer") {

        if(arguments.size() < 1 || !arguments[0]->IsString()) {
            // Invalid arguments //
            exception = "Invalid arguments passed, expected: string";
            return true;
        }

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "connectToServer");
        args->SetString(1, arguments[0]->GetStringValue());

        Owner->SendCustomExtensionMessage(message);
        return true;
    } else if(name == "disconnectFromServer") {
        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "disconnectFromServer");

        Owner->SendCustomExtensionMessage(message);
        return true;
    } else if(name == "enterPlanetEditor") {
        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "enterPlanetEditor");

        Owner->SendCustomExtensionMessage(message);
        return true;
    } else if(name == "editPlanet") {

        if(arguments.size() < 2 || !arguments[0]->IsString() ||
            !arguments[1]->IsDouble()) {
            // Invalid arguments //
            exception = "Invalid arguments passed, expected: string, double";
            return true;
        }

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "editPlanet");
        args->SetString(1, arguments[0]->GetStringValue());
        args->SetDouble(2, arguments[1]->GetDoubleValue());

        Owner->SendCustomExtensionMessage(message);
        return true;
    } else if(name == "pause") {

        if(arguments.size() < 1 || !arguments[0]->IsBool()) {
            // Invalid arguments //
            exception = "Invalid arguments passed, expected: bool";
            return true;
        }

        auto message = CefProcessMessage::Create("Custom");
        auto args = message->GetArgumentList();
        args->SetString(0, "pause");
        args->SetBool(1, arguments[0]->GetBoolValue());

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

    } else if(customType == "freebuildEditorButtonClicked") {

        ThriveGame::Get()->enableFreebuild();
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
    } else if(customType == "connectToServer") {

        ThriveGame::Get()->connectToServer(args->GetString(1));
        return true;
    } else if(customType == "disconnectFromServer") {

        ThriveGame::Get()->disconnectFromServer(true);
        return true;

    } else if(customType == "enterPlanetEditor") {

        ThriveGame::Get()->enterPlanetEditor();
        return true;

    } else if(customType == "editPlanet") {

        ThriveGame::Get()->editPlanet(args->GetString(1), args->GetDouble(2));
        return true;

    } else if(customType == "pause") {

        ThriveGame::Get()->pause(args->GetBool(1));
        return true;
    }

    // Not ours
    return false;
}
