{
  description = "Nix Flake for Thrive Development";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-23.11";

  outputs = { self, nixpkgs, ... }:
    let
      pkgs = import nixpkgs {
        system = "x86_64-linux";
      };
    in
    {
      # Launch into the shell environment with `nix develop`
      # Building and running is possible by following setup_instructions.md
      devShells.x86_64-linux.default =
        pkgs.mkShell {
          packages = with pkgs; [
            # Git lfs needs to be enabled in .config
            git
            git-lfs
            dotnet-sdk_8
            self.packages.x86_64-linux.godot4-dotnet-bin
            # For compiling native libraries
            stdenv
            clang
            lld
            cmake
          ];
        };

      packages.x86_64-linux.godot4-dotnet-bin = pkgs.stdenv.mkDerivation rec {
          pname = "godot4-dotnet-bin";
          version = "4.2.2-rc2";

          src = pkgs.fetchurl {
            url = "https://github.com/godotengine/godot-builds/releases/download/${version}/Godot_v${version}_mono_linux_x86_64.zip";
            sha256 = "sha256-bwx1kRzzNMqu9AipziIjla4alcRrcUDV1S7lUcxXGwY=";
          };

          nativeBuildInputs = with pkgs; [autoPatchelfHook makeWrapper unzip];

          buildInputs = with pkgs; with pkgs.xorg; [
            udev
            alsaLib
            fontconfig
            libxkbcommon
            libXcursor
            libXinerama
            libXrandr
            libXrender
            libXext
            libX11
            libXi
            libpulseaudio
            libGL
            zlib
            glslang # or shaderc
            vulkan-headers
            vulkan-loader
            vulkan-validation-layers
            dbus
          ];

          libraries = pkgs.lib.makeLibraryPath buildInputs;

          unpackCmd = "";

          installPhase = ''
            mkdir -p $out/bin $out/opt/godot

            install -m 0755 Godot_v${version}_mono_linux.x86_64 $out/opt/godot/Godot_v${version}_mono_linux.x86_64
            cp -r GodotSharp $out/opt/godot
            ln -s $out/opt/godot/Godot_v${version}_mono_linux.x86_64 $out/bin/godot
          '';

          postFixup = ''
            wrapProgram $out/bin/godot \
              --set LD_LIBRARY_PATH ${libraries}
          '';
        };
      };
}
