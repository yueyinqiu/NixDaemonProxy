# NixDaemonProxy

Switch the proxy used by the **Nix daemon** (`nix-daemon`) on the fly, without rebuilding your NixOS configuration or editing `/etc` by hand.

`nix-daemon` is normally started by systemd, and its proxy settings come from its service environment — so changing them used to require a configuration rebuild. `NixDaemonProxy` solves this by running a small local proxy that always stays in the environment, while the *upstream* it forwards to can be switched at runtime with a single command.

## Why not just configure the proxy in NixOS?

- Every switch (different proxy, different credentials, back to direct) would mean editing the config, rebuilding, and probably rebooting.
- A proxy is often **personal and private** (e.g. your own Clash/V2Ray with credentials), while the NixOS config is usually shared and admin-maintained — private details don't belong there.

So the NixOS side stays proxy-free and just runs the local proxy server. *Which* upstream to use is decided at runtime by any user in the `nix-daemon-proxy` group.

> **Caveat:** the *setting* is per-user, but the *effect* is global — there's only one `nix-daemon` per machine. While your proxy is active, other users building with that daemon go through it too. Unavoidable with a single shared daemon.

## How it works

The systemd service of `nix-daemon` always points at a **local proxy server** (`NixDaemonProxy.Server`). The local proxy is the *only* hop configured in the daemon's environment, so nothing has to be rewritten to switch proxies:

```
 nix-daemon
     │   http_proxy = http://user:password@127.0.0.1:<port>/
     ▼
 ┌─────────────────────────────────────────────┐
 │ NixDaemonProxy.Server (local proxy)         │
 │   listens on 127.0.0.1, Basic-auth protected│
 │   control socket: /run/nix-daemon-proxy.sock│
 └─────────────────────────────────────────────┘
     │  upstream switchable at runtime
     ▼
 external proxy (HTTP / SOCKS4 / SOCKS5)  ──►  internet
```

The flow is:

1. The server starts, writes a systemd **drop-in** for `nix-daemon.service` that sets `all_proxy`, `http_proxy` and `https_proxy` to the local proxy, and restarts `nix-daemon`.
2. You run the **client** (`NixDaemonProxy.Client`), which talks to the control socket over a Unix domain socket and tells the server which *upstream* proxy to use (HTTP, SOCKS4 or SOCKS5).
3. The server switches its upstream and everything `nix-daemon` does is proxied through your chosen proxy — instantly.
4. When the server stops, it removes the drop-in and restarts `nix-daemon` to restore direct access.

This means you can switch between a VPN, a public proxy, or "direct" as often as you like — no rebuild, no reboot, no manual file editing.

## Installation

### NixOS

Nix packaging lives in the separate [`NixDaemonProxy-Nix`](https://github.com/yueyinqiu/NixDaemonProxy-Nix) repository, which exposes packages and a NixOS module. Add it as a flake input and enable the module:

```nix
{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    nix-daemon-proxy.url = "github:yueyinqiu/NixDaemonProxy-Nix";
  };

  outputs = { nixpkgs, nix-daemon-proxy, ... }: {
    nixosConfigurations.your-host = nixpkgs.lib.nixosSystem {
      system = "x86_64-linux";
      modules = [
        nix-daemon-proxy.nixosModules.nix-daemon-proxy
        {
          services.nix-daemon-proxy.enable = true;

          # optional: also install a wrapped client on the system PATH
          services.nix-daemon-proxy.installClient = true;
        }
      ];
    };
  };
}
```

The module runs the server as a `systemd` service (`nix-daemon-proxy-server`) and creates the `nix-daemon-proxy` group.

> To avoid building from source, you can use the `yueyinqiu` Cachix binary cache:
>
> ```nix
> {
>   nix.settings.extra-substituters = [
>     "https://yueyinqiu.cachix.org"
>   ];
>   nix.settings.extra-trusted-public-keys = [
>     "yueyinqiu.cachix.org-1:iooLFYpS7e6KAU4+QM5Zoj6Tq76jRGo+kjeAbu8JxAc="
>   ];
> }
> ```

#### Module options

Options under `services.nix-daemon-proxy`:

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `enable` | bool | `false` | Whether to enable NixDaemonProxy |
| `package` | package | the flake's `nix-daemon-proxy-server` | The server package to run |
| `installClient` | bool | `false` | Also install a wrapped client into `environment.systemPackages` |
| `group` | str | `"nix-daemon-proxy"` | Group whose members can access the control socket |
| `controlSocket` | str | `"/run/nix-daemon-proxy.sock"` | Unix socket path the control server listens on |
| `proxyPort` | nullOr port | `null` | TCP port the local proxy listens on (`127.0.0.1`); random when `null` |
| `nixDaemonService` | nullOr str | `"nix-daemon"` | systemd service to configure; `null` disables daemon configuration |

The proxy password is always generated randomly per boot by the server.

### Other distributions

If you don't use NixOS, just create the `nix-daemon-proxy` group and run the server as root. Take Ubuntu as an example:

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 wget unzip

wget -O /tmp/NixDaemonProxy.Server.zip https://github.com/yueyinqiu/NixDaemonProxy/releases/latest/download/NixDaemonProxy.Server.zip
sudo mkdir -p /opt/nix-daemon-proxy
sudo unzip /tmp/NixDaemonProxy.Server.zip -d /opt/nix-daemon-proxy
rm /tmp/NixDaemonProxy.Server.zip

sudo groupadd -f nix-daemon-proxy

sudo tee /etc/systemd/system/nix-daemon-proxy-server.service > /dev/null << 'EOF'
[Unit]
Description=Nix Daemon Proxy Server

[Service]
ExecStart=/usr/bin/dotnet /opt/nix-daemon-proxy/NixDaemonProxy.Server.dll
Restart=on-failure
RestartSec=5s
ExecStartPre=/bin/rm -f /run/nix-daemon-proxy.sock

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable --now nix-daemon-proxy-server

sudo usermod -aG nix-daemon-proxy <trusted-users>    # won't take effect in current login session.
```

## Server options

The server binary accepts a few options (defaults shown):

| Option              | Default                    | Meaning                                                    |
| ------------------- | -------------------------- | ---------------------------------------------------------- |
| `--control-socket`  | `/run/nix-daemon-proxy.sock` | Unix socket path the control HTTP server listens on       |
| `--control-group`   | `nix-daemon-proxy`         | Group that gets read/write access to the control socket    |
| `--proxy-password`  | *(random, generated)*      | Basic-auth password for the local proxy. Random per boot by default; set it explicitly to keep it stable. Can also be supplied via the `NIX_DAEMON_PROXY_SERVER_SECRET_ARGUMENTS_PROXY_PASSWORD` environment variable |
| `--proxy-port`      | `0` *(random)*             | TCP port the local proxy listens on (`127.0.0.1`)          |
| `--nix-daemon-service` | `nix-daemon`             | Name of the systemd service to configure. Pass the flag without a value (`--nix-daemon-service`) to disable daemon configuration |

## Client setup

The client (`NixDaemonProxy.Client`) is exposed by the same flake. The simplest way is to set `services.nix-daemon-proxy.installClient = true` (see above), which installs a wrapped client named `nix-daemon-proxy` with the control socket already preset.

To install the plain client per-user instead, e.g. in `home.packages`:

```nix
nix-daemon-proxy.packages.${system}.nix-daemon-proxy-client
```

Or run it directly without installing anything:

```console
$ nix run github:yueyinqiu/NixDaemonProxy-Nix#nix-daemon-proxy-client -- http -H 127.0.0.1 -P 7890
```

Or enter a shell that has it on `PATH`:

```console
$ nix shell github:yueyinqiu/NixDaemonProxy-Nix#nix-daemon-proxy-client
```

Or install it into your profile:

```console
$ nix profile install github:yueyinqiu/NixDaemonProxy-Nix#nix-daemon-proxy-client
```

## Usage

The client talks to the server over the control socket, so **you must be a member of the `nix-daemon-proxy` group** to use it.

> **Security:** never pass a password on the command line. Options like `-p` show up in `ps` and are visible to any local user. Use the `NIX_DAEMON_PROXY_CLIENT_SECRET_ARGUMENTS_PASSWORD` environment variable instead — the command line takes precedence if both are set:
>
> ```sh
> export NIX_DAEMON_PROXY_CLIENT_SECRET_ARGUMENTS_PASSWORD=secret
> NixDaemonProxy.Client http -H 127.0.0.1 -P 7890 -u user
> ```
>
> `from-json` reads its JSON from stdin, so it is safe to put passwords there.

### Switch to an HTTP proxy

```sh
NixDaemonProxy.Client http -H 127.0.0.1 -P 7890
```

### Switch to a SOCKS5 proxy

```sh
NixDaemonProxy.Client socks5 -H 192.168.1.2 -P 1080
```

### Switch back to direct access

```sh
NixDaemonProxy.Client direct
```

### Advanced usage

Use `from-json` for full control — e.g. chaining two proxies (`NextHop`), or setting DNS proxy / localhost-bypass flags. It maps to the `Proxy` record in [`src/NixDaemonProxy.Interface/Proxy.cs`](src/NixDaemonProxy.Interface/Proxy.cs), with `ProxyType` being one of `Http`, `Socks4` or `Socks5`:

```sh
NixDaemonProxy.Client from-json <<'EOF'
{
  "ProxyType": "Socks5",
  "HostName": "192.168.1.2",
  "Port": 1080,
  "UserName": null,
  "Password": null,
  "ProxyDnsRequests": true,
  "BypassLocalhost": false,
  "NextHop": {
    "ProxyType": "Http",
    "HostName": "192.168.1.3",
    "Port": 7890,
    "UserName": "user",
    "Password": "secret",
    "ProxyDnsRequests": true,
    "BypassLocalhost": true,
    "NextHop": null
  }
}
EOF
```

### Client command reference

| Command | Options | Description |
| --- | --- | --- |
| `http` | `-H <host>`, `-P <port>`, `-u <user>`, `-p <password>`, `--[no-]proxy-dns-requests`, `--bypass-localhost`, `--control-socket <path>` | Use an HTTP(S) proxy upstream |
| `socks5` | same as `http` | Use a SOCKS5 proxy upstream |
| `direct` | `--control-socket <path>` | Remove the upstream proxy (go direct) |
| `from-json` | `--control-socket <path>` | Read a JSON `Proxy` record from stdin and set it as the upstream (supports `NextHop` chains) |

Shared options:

- `--host-name` / `-H`: upstream proxy host.
- `--port` / `-P`: upstream proxy port.
- `--user-name` / `-u`: upstream proxy username (optional).
- `--password` / `-p`: upstream proxy password (optional). Can also be supplied via the `NIX_DAEMON_PROXY_CLIENT_SECRET_ARGUMENTS_PASSWORD` environment variable; the command line takes precedence.
- `--proxy-dns-requests` (default `true`): let the proxy resolve DNS instead of the local machine.
- `--bypass-localhost` (default `false`): do not proxy requests to localhost.
- `--control-socket` (default `/run/nix-daemon-proxy.sock`): path of the server's control socket, in case it was changed.

## Rescue: proxy switched but not working

This should be next to impossible, but just in case: the switch to the proxy succeeded (`nix-daemon` restarted with the proxy env vars), but then the server crashed — and keeps crashing on restart. The control socket is gone, and `nix-daemon` is pointed at a dead proxy, so you can't even rebuild NixOS.

Don't worry. The proxy only reaches `nix-daemon` through a runtime systemd **drop-in**, so remove it as root and restart the daemon will save you:

```sh
sudo systemctl stop nix-daemon-proxy-server
sudo rm /run/systemd/system/nix-daemon.service.d/nix-daemon-proxy-02f2de3ae7134c999a969d2b8f6f2f46.conf
sudo systemctl daemon-reload
sudo systemctl restart nix-daemon
```

## Security notes

- The local proxy is bound to `127.0.0.1` and protected by Basic auth, but only the server knows the (random) password.
- Access to the control socket is restricted to the `nix-daemon-proxy` group, so only members can switch the proxy.

## License

[MIT](LICENSE)

---

The documentation is AI-generated.
