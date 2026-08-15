# NixDaemonProxy

Sample server configuration (put it in your NixOS configuration):

```nix
{ nur, ... }:
{
  # users in nix-daemon-proxy group will be able to access the service
  users.groups.nix-daemon-proxy = { };

  systemd.services.nix-daemon-proxy-server = {
    wantedBy = [ "multi-user.target" ];

    serviceConfig = {
      ExecStart = "${nur.yueyinqiu.nix-daemon-proxy-server}/bin/NixDaemonProxy.Server";
      Restart = "on-failure";
      RestartSec = "5s";

      # asp.net will try to find/watch the configuration files.
      # so don't forget to change its working directory, 
      # otherwise it will search your full /nix/store...
      PrivateTmp = true;
      WorkingDirectory = "/tmp";
    };
  };
}
```

Here `nur` is `github:nix-community/NUR#legacyPackages.<your-system>.repos`.

> You could use my cachix to avoid building it from source:
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

> If you don't use NixOS, just create the `nix-daemon-proxy` group and run the server as root.

Sample client configuration:

```nix
{ nur, ... }:
{
  home.packages = [
    nur.yueyinqiu.nix-daemon-proxy-client
  ];
}
```

Then you can use the following command to change the proxy for nix-daemon (if you are in `nix-daemon-proxy` group):

```sh
NixDaemonProxy.Client http -H 127.0.0.1 -P 7890
```
