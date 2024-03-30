{
  description = "Nix Flake for Thrive Development";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-23.11";
    # PR for godot4-mono 4.2.1 https://github.com/NixOS/nixpkgs/pull/285941
    # TODO: switch to master once merged
    nixpkgs-godot.url = "github:ilikefrogs101/nixpkgs/master";
  };

  outputs = { self, nixpkgs, nixpkgs-godot, ... }:
    let
      supportedSystems = [ "x86_64-linux" "x86_64-darwin" "aarch64-linux" "aarch64-darwin" ];
      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
      nixpkgsFor = forAllSystems (system: import nixpkgs { inherit system; });
      nixpkgs-godotFor = forAllSystems (system: import nixpkgs { inherit system; });
    in
    {
      # Launch into the shell environment with `nix develop`
      # Building and running is possible by following setup_instructions.md
      devShells = forAllSystems (system:
        let
          pkgs = nixpkgsFor.${system};
          pkgs-godot = nixpkgs-godotFor.${system};
        in
        {
          default = pkgs.mkShell {
            packages = with pkgs; [
                # Git lfs needs to be enabled in .config
                git
                git-lfs
                dotnet-sdk_8
                ((pkgs-godot.callPackage "${nixpkgs-godot}/pkgs/development/tools/godot/4/mono" { }).override { withTouch = false; })
                # For compiling native libraries
                stdenv
                clang
                lld
                cmake
                # For packaging manually
                zip
                p7zip
                # For Localization
                poedit
                gettext
              ];
          };
        });
    };
}
