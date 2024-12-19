{
  description = "Nix Flake for Thrive Development";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixpkgs-unstable";
  };

  outputs =
    {
      nixpkgs,
      ...
    }:
    let
      supportedSystems = [
        "x86_64-linux"
        "x86_64-darwin"
        "aarch64-linux"
        "aarch64-darwin"
      ];
      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
    in
    {
      # Launch into the shell environment with `nix develop`
      # Building and running is possible by following setup_instructions.md
      devShells = forAllSystems (
        system:
        let
          pkgs = import nixpkgs {
            inherit system;
            config = {
              permittedInsecurePackages = [
                "dotnet-sdk-6.0.428"
              ];
            };
          };
        in
        {
          default = pkgs.mkShell {
            packages = with pkgs; [
              # Godot
              godot_4-mono

              # Make "godot" be callable from path
              (pkgs.writeShellScriptBin "godot" "exec -a $0 ${godot_4-mono}/bin/godot4-mono $@")

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

              # Profiling
              flamegraph

              # Runtime Deps
              mono
              dotnet-sdk_8
            ];
          };
        }
      );
    };
}
