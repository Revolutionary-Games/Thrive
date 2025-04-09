#!/bin/bash

# Check for command-line argument
godot_bin=""
filtered_args=""

# Process all arguments with a more compatible approach
while [ $# -gt 0 ]; do
    if [ "$1" = "--godot_bin" ] && [ $# -gt 1 ]; then
        # Get the next argument as the value
        godot_bin="$2"
        shift 2
    else
        # Keep non-godot_bin arguments for passing to Godot
        filtered_args="$filtered_args $1"
        shift
    fi
done

# If --godot_bin wasn't provided, fallback to environment variable
if [ -z "$godot_bin" ]; then
    godot_bin="$GODOT_BIN"
fi

# Check if we have a godot_bin value from any source
if [ -z "$godot_bin" ]; then
    echo "Godot binary path is not specified."
    echo "Please either:"
    echo "  - Set the environment variable: export GODOT_BIN=/path/to/godot"
    echo "  - Or use the --godot_bin argument: --godot_bin /path/to/godot"
    exit 1
fi

# Check if the Godot binary exists and is executable
if [ ! -f "$godot_bin" ]; then
    echo "Error: The specified Godot binary '$godot_bin' does not exist."
    exit 1
fi

if [ ! -x "$godot_bin" ]; then
    echo "Error: The specified Godot binary '$godot_bin' is not executable."
    exit 1
fi

# Get Godot version and check if it's a .NET build
GODOT_VERSION=$("$godot_bin" --version)
if echo "$GODOT_VERSION" | grep -i "mono" > /dev/null; then
    echo "Godot .NET detected"
    echo "Compiling c# classes ... Please Wait"
    dotnet build --debug
    echo "done $?"
fi

# Run the tests with the filtered arguments
"$godot_bin" --path . -s -d res://addons/gdUnit4/bin/GdUnitCmdTool.gd $filtered_args
exit_code=$?
echo "Run tests ends with $exit_code"

# Run the copy log command
"$godot_bin" --headless --path . --quiet -s res://addons/gdUnit4/bin/GdUnitCopyLog.gd $filtered_args > /dev/null
exit_code2=$?
exit $exit_code
