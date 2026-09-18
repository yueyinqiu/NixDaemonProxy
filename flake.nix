{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs =
    { nixpkgs, ... }:
    let
      forAllSystems = nixpkgs.lib.genAttrs nixpkgs.lib.systems.flakeExposed;
    in
    {
      devShells = forAllSystems (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = pkgs.mkShell {
            packages = [
              pkgs.dotnetCorePackages.sdk_10_0
              (pkgs.writeShellScriptBin "dev-publish" ''
                set -euo pipefail
                mkdir -p publish
                temp=$(mktemp -d -p publish)

                dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj -c Release -o "$temp/NixDaemonProxy.Server"
                ouch compress "$temp/NixDaemonProxy.Server"/* "$temp/NixDaemonProxy.Server.zip"

                dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj -c Release -o "$temp/NixDaemonProxy.Client"
                ouch compress "$temp/NixDaemonProxy.Client"/* "$temp/NixDaemonProxy.Client.zip"
              '')
            ];
            shellHook = ''
              export DOTNET_ROOT="${pkgs.dotnetCorePackages.sdk_10_0}/share/dotnet"
            '';
          };
        }
      );
    };
}
