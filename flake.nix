{
  description = "Nix Flake for Thrive";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-23.11";

  outputs = { self, nixpkgs, ... }:
    let
      pkgs = import nixpkgs {
        system = "x86_64-linux";
      };
    in
    {
      # Launch into the shell environment with `nix develop`
      # Building manually in this dev env can be done by following setup_instructions.md
      devShells.x86_64-linux.default =
        pkgs.mkShell {
          packages = with pkgs; [
            # Git lfs needs to be enabled in .config
            git
            git-lfs
            dotnet-sdk_8
            p7zip
            (nuget-to-nix.override { dotnet-sdk = dotnet-sdk_8; }) # This can update the deps.nix
            jq
            gawk
            curl
            gnugrep
            xq-xml
            self.packages.x86_64-linux.godot4-dotnet-bin
          ];
        };

      # Can't import resources from command line
      # https://github.com/godotengine/godot-proposals/issues/1362

      # How to build Thrive
      # - clone repo WITH SUBMODULES!
      # - git lfs pull
      # - dotnet restore / fetch nuget
      # - Godot compile
      # - Dotnet Fetch Install

      # Fully build Thrive
      # packages.x86_64-linux.default = pkgs.buildDotnetModule rec {
      #   pname = "Thrive";
      #   version = "0.0.0"; # TODO: the update to godot 4 does not have a release yet
      #   src = pkgs.fetchgit {
      #     url = "https://github.com/Revolutionary-Games/${pname}";
      #     branchName = "13272caddcca2e00f7446188b5b0d1522f24d9b6";
      #     sha256 = "sha256-qmtY00fnc2MB0jWcGcwd7l2NNgHANu+tPlfLfMZzqoU=";
      #     fetchLFS = true;
      #     fetchSubmodules = true;
      #   };

      #   projectFile = ./Thrive.sln;
      #   nugetDeps = ./deps.nix;
      #   dotnet-sdk = pkgs.dotnet-sdk_8;
      #   dotnet-runtime = pkgs.dotnet-runtime_8;

      #   # configurePhase = ''
      #   #   dotnet run --project Scripts -- make-project-valid
      #   #   dotnet restore Thrive.sln
      #   # '';

      #   # # godot --headless --export-release "Linux/X11"
      #   # # dotnet run --project Scripts -- native Fetch
      #   # buildPhase = ''
      #   #   dotnet run --project Scripts -- check compile
      #   #   dotnet run --project Scripts -- native Build
      #   # '';

      #   # installPhase = ''
      #   #   dotnet run --project Scripts -- package --dehydrated
      #   # '';

      #   nativeBuildInputs = with pkgs; [
      #     self.packages.x86_64-linux.godot4-dotnet-bin
      #   ];
      # };

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