@ECHO OFF
CLS

IF NOT DEFINED GODOT_BIN (
	ECHO "GODOT_BIN is not set."
	ECHO "Please set the environment variable 'setx GODOT_BIN <path to godot.exe>'"
	EXIT /b -1
)

REM scan if Godot mono used and compile c# classes
for /f "tokens=5 delims=. " %%i in ('%GODOT_BIN% --version') do set GODOT_TYPE=%%i
IF "%GODOT_TYPE%" == "mono" (
	ECHO "Godot mono detected"
	ECHO Compiling c# classes ... Please Wait
	dotnet build --debug
	ECHO done %errorlevel%
)

%GODOT_BIN% -s -d res://addons/gdUnit4/bin/GdUnitCmdTool.gd %*
SET exit_code=%errorlevel%
%GODOT_BIN% --headless --quiet -s -d res://addons/gdUnit4/bin/GdUnitCopyLog.gd %*

ECHO %exit_code%

EXIT /B %exit_code%
