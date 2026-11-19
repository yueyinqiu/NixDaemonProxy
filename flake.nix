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
              pkgs.clang
              pkgs.zip
              (pkgs.writeShellScriptBin "dev-publish" ''
                set -euo pipefail
                mkdir -p publish
                temp=$(mktemp -d -p publish)

                # linux-x64 (native AOT)
                dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj -c Release -r linux-x64 -p:PublishAot=true -o "$temp/NixDaemonProxy.Server-linux-x64"
                (cd "$temp/NixDaemonProxy.Server-linux-x64" && zip -rq ../NixDaemonProxy.Server-linux-x64.zip .)
                dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj -c Release -r linux-x64 -p:PublishAot=true -o "$temp/NixDaemonProxy.Client-linux-x64"
                (cd "$temp/NixDaemonProxy.Client-linux-x64" && zip -rq ../NixDaemonProxy.Client-linux-x64.zip .)

                # linux-arm64 (self-contained)
                dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj -c Release -r linux-arm64 --self-contained -p:PublishTrimmed=true -o "$temp/NixDaemonProxy.Server-linux-arm64"
                (cd "$temp/NixDaemonProxy.Server-linux-arm64" && zip -rq ../NixDaemonProxy.Server-linux-arm64.zip .)
                dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj -c Release -r linux-arm64 --self-contained -p:PublishTrimmed=true -o "$temp/NixDaemonProxy.Client-linux-arm64"
                (cd "$temp/NixDaemonProxy.Client-linux-arm64" && zip -rq ../NixDaemonProxy.Client-linux-arm64.zip .)

                # framework-dependent
                dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj -c Release -o "$temp/NixDaemonProxy.Server"
                (cd "$temp/NixDaemonProxy.Server" && zip -rq ../NixDaemonProxy.Server.zip .)
                dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj -c Release -o "$temp/NixDaemonProxy.Client"
                (cd "$temp/NixDaemonProxy.Client" && zip -rq ../NixDaemonProxy.Client.zip .)
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