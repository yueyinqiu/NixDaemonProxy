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
              (pkgs.writeShellApplication {
                name = "dev-publish";
                runtimeInputs = [
                  pkgs.dotnetCorePackages.sdk_10_0
                  pkgs.clang
                  pkgs.zip
                ];
                text = ''
                  if [ ! -f NixDaemonProxy.slnx ]; then
                    echo "error: dev-publish must be run from the project root (missing NixDaemonProxy.slnx)" >&2
                    exit 1
                  fi

                  mkdir -p publish
                  pub="$(pwd)/publish"
                  temp="$(mktemp -d)"

                  # linux-x64 (native AOT, single file)
                  dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj \
                    -c Release -r linux-x64 -p:PublishAot=true \
                    -o "$temp/server-x64"
                  cp "$temp/server-x64/NixDaemonProxy.Server" "$pub/NixDaemonProxy.Server-linux-x64"
                  dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj \
                    -c Release -r linux-x64 -p:PublishAot=true \
                    -o "$temp/client-x64"
                  cp "$temp/client-x64/NixDaemonProxy.Client" "$pub/NixDaemonProxy.Client-linux-x64"

                  # linux-arm64 (self-contained, single file)
                  dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj \
                    -c Release -r linux-arm64 --self-contained \
                    -p:PublishTrimmed=true -p:PublishSingleFile=true \
                    -o "$temp/server-arm64"
                  cp "$temp/server-arm64/NixDaemonProxy.Server" "$pub/NixDaemonProxy.Server-linux-arm64"
                  dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj \
                    -c Release -r linux-arm64 --self-contained \
                    -p:PublishTrimmed=true -p:PublishSingleFile=true \
                    -o "$temp/client-arm64"
                  cp "$temp/client-arm64/NixDaemonProxy.Client" "$pub/NixDaemonProxy.Client-linux-arm64"

                  # framework-dependent (directory, zip)
                  dotnet publish src/NixDaemonProxy.Server/NixDaemonProxy.Server.csproj \
                    -c Release \
                    -o "$temp/server"
                  (cd "$temp/server" && zip -rq "$pub/NixDaemonProxy.Server.zip" .)
                  dotnet publish src/NixDaemonProxy.Client/NixDaemonProxy.Client.csproj \
                    -c Release \
                    -o "$temp/client"
                  (cd "$temp/client" && zip -rq "$pub/NixDaemonProxy.Client.zip" .)

                  rm -rf "$temp"
                '';
              })
            ];
            shellHook = ''
              export DOTNET_ROOT="${pkgs.dotnetCorePackages.sdk_10_0}/share/dotnet"
            '';
          };
        }
      );
    };
}