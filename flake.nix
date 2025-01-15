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
            # Dotnet 6 is EOL, but godot still needs it
            # This can be removed in 4.4
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

              # For compiling native libraries
              cmake
              clang_18
              lld_18

              # For packaging manually
              zip
              p7zip

              # For Localization
              poedit
              gettext

              # Profiling
              flamegraph

              # Dotnet
              dotnet-sdk_9

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
