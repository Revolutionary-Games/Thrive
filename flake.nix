{
  description = "Nix Flake for Thrive Development";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";

    # PR for godot4-mono 4.2.1 https://github.com/NixOS/nixpkgs/pull/285941
    # TODO: switch to master once merged
    nixpkgs-godot.url = "github:GameDungeon/nixpkgs/Godot-4.2.2";
  };

  outputs = {
    self,
    nixpkgs,
    nixpkgs-godot,
    flake-utils,
  }:
    flake-utils.lib.eachDefaultSystem (
      system: let
        pkgs = import nixpkgs {inherit system;};
        pkgs-godot = import nixpkgs-godot {inherit system;};
        fhs = pkgs.buildFHSUserEnv {
          name = "fhs-shell";

          targetPkgs = pkgs:
            with pkgs; [
              # Godot
              dotnet-sdk_8
              ((pkgs-godot.callPackage "${nixpkgs-godot}/pkgs/development/tools/godot/4/mono" {}).override {
                withTouch = false;
                withDebug = "no"; # Set to yes if you wish to do profiling
              })

              # For compiling native libraries
              cmake
              clang_14
              lld_17

              # For packaging manually
              zip
              p7zip

              # For Localization
              poedit
              gettext

              # Runtime dependencies
              xorg.libX11
              xorg.libXcursor
              xorg.libXext
              fontconfig
              libxkbcommon
              xorg.libXrandr
              xorg.libXrender
              xorg.libXinerama
              xorg.libXi
              mesa
              vulkan-loader
              vulkan-headers
              libglvnd
              dbus
              alsaLib
              pulseaudio
              icu
            ];
        };
      in {
        devShell = fhs.env;
      }
    );
}
