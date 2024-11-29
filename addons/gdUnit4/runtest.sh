#!/bin/sh

if [ -z "$GODOT_BIN" ]; then
    echo "'GODOT_BIN' is not set."
    echo "Please set the environment variable  'export GODOT_BIN=/Applications/Godot.app/Contents/MacOS/Godot'"
    exit 1
fi

"$GODOT_BIN" --path . -s -d res://addons/gdUnit4/bin/GdUnitCmdTool.gd $*
exit_code=$?
echo "Run tests ends with $exit_code"

"$GODOT_BIN" --headless --path . --quiet -s -d res://addons/gdUnit4/bin/GdUnitCopyLog.gd $* > /dev/null
exit_code2=$?
exit $exit_code
