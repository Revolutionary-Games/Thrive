{
  description = "Nix Flake for Thrive Development";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs {
          inherit system;
          config = {
            # Dotnet 6 is EOL, but somthing here still needs it
            permittedInsecurePackages = [
              "dotnet-sdk-6.0.428"
            ];
          };
        };
        fhs = pkgs.buildFHSEnv {
          name = "fhs-shell";

          targetPkgs =
            pkgs: with pkgs; [
              # Godot
              (godot_4.override {
                withMono = true;
                dotnet-sdk_8 = dotnet-sdk_9;
              })

              # Make "godot" be callable from path
              (pkgs.writeShellScriptBin "godot" "exec -a $0 ${godot_4-mono}/bin/godot4-mono $@")

              # For compiling native libraries
              cmake
              clang_18

              # For packaging manually
              zip
              p7zip

              # For Localization
              poedit
              gettext

              # Profiling
              flamegraph

              # Runtime dependencies
              dotnet-sdk_9
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
              alsa-lib
              pulseaudio
              icu
            ];
        };
      in
      {
        devShell = fhs.env;
      }
    );
}
