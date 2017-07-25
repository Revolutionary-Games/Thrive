#include "thrive_include.h"
#include "ThriveGame.h"
#include "resource.h"

#include "Define.h"

// Breakpad is used to detect and report crashes
#ifdef LEVIATHAN_USING_BREAKPAD
#ifdef __linux
#include "client/linux/handler/exception_handler.h"
#elif defined(_WIN32)
#include "client/windows/handler/exception_handler.h"
#else
#error no breakpad on platform
#endif
#endif //USE_BREAKPAD

using namespace thrive;

// Don't look at the mess ahead, just set the variables in your cmake file //

#if LEVIATHAN_USING_BREAKPAD
#ifdef _WIN32
#ifndef _DEBUG
bool DumpCallback(const wchar_t* dump_path,
    const wchar_t* minidump_id, void* context, EXCEPTION_POINTERS* exinfo,
    MDRawAssertionInfo* assertion, bool succeeded)
{
    
    const string path = Convert::WstringToString(dump_path);
    
    printf("Dump path: %s\n", path.c_str());
    
    return succeeded;
}
#endif //_DEBUG
#else
bool DumpCallback(const google_breakpad::MinidumpDescriptor& descriptor, void* context,
    bool succeeded)
{
    printf("Dump path: %s\n", descriptor.path());
    return succeeded;
}
#endif
#endif //USE_BREAKPAD

#ifdef _WIN32
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine,
    int nCmdShow)
{
#if defined(DEBUG) | defined(_DEBUG)
    _CrtSetReportMode( _CRT_ASSERT, _CRTDBG_MODE_DEBUG);
#endif

#else
int main(int argcount, char* args[]){
#endif
    
#ifdef _WIN32
    int argcount = 1;
    char* args[] = { lpCmdLine };
#else
    // We need to skip the program name
    args += 1;
    --argcount;
#endif
    
    int Return = 0;
    
#ifdef _WIN32
    HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    if(SUCCEEDED(CoInitialize(NULL))){
#else

#endif

#if LEVIATHAN_USING_BREAKPAD
    // Crash handler //
#ifdef _WIN32
#ifndef _DEBUG
    google_breakpad::ExceptionHandler ExceptionHandler(L"C://tmp", NULL, DumpCallback, NULL, 
        google_breakpad::ExceptionHandler::HANDLER_ALL,
        (MINIDUMP_TYPE)(MiniDumpNormal & MiniDumpWithThreadInfo),
        (const wchar_t*)nullptr, NULL);
        
#endif //_DEBUG
#else
    google_breakpad::MinidumpDescriptor descriptor("/tmp");

    google_breakpad::ExceptionHandler ExceptionHandler(descriptor, NULL, DumpCallback, NULL,
        true, -1);
#endif
#endif //USE_BREAKPAD
    
    // Create program object //
    ThriveGame app;

    std::unique_ptr<AppDef> ProgramDefinition(AppDef::GenerateAppdefine("ThriveLog",
            "./EngineConf.conf", 
        "./ThriveGame.conf", "./ThriveKeybindings.conf", &ThriveGame::CheckGameConfigurationVariables,
        &ThriveGame::CheckGameKeyConfigVariables));

    // Fail if no definition could be created //
    if(!ProgramDefinition){

        std::cout << "FATAL: failed to create AppDefine" << std::endl;
        return 2;
    }
    
    
    // customize values //
#ifdef _WIN32
    ProgramDefinition->SetHInstance(hInstance);
#endif
    ProgramDefinition->SetMasterServerParameters(MasterServerInformation("ThriveMasters.txt", "Thrive_" GAME_VERSIONS, "http://revolutionarygamesstudio.com/", "/Thrive/MastersList.png", "ThriveAccountCrecentials.txt", false)).
        SetApplicationIdentification(
        "Thrive game version " GAME_VERSIONS, "Thrive",
        "GAME_VERSIONS");
    
    // Create window last //
    ProgramDefinition->StoreWindowDetails(ThriveGame::GenerateWindowTitle(), true,
#ifdef _WIN32
        LoadIcon(hInstance, MAKEINTRESOURCE(IDI_ICON1)),
#endif
        &app);

    if(!app.PassCommandLine(argcount, args)){

        std::cout << "Error: Invalid Command Line arguments. Shutting down" << std::endl;
        return 3;
    }
    
    if(app.Initialize(ProgramDefinition.get())){

        // this is where the game should customize the engine //
        app.CustomizeEnginePostLoad();

        LOG_INFO("Engine successfully initialized");
        Return = app.RunMessageLoop();
    } else {
        LOG_ERROR("App init failed, closing");
        app.ForceRelease();
        Return = 5;
    }
#ifdef _WIN32
    }
    //_CrtDumpMemoryLeaks();
    CoUninitialize();
#endif

    return Return;
}
