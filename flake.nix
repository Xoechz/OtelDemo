{
  description = "Dotnet NixShell";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { nixpkgs, ... }: {
    devShells.x86_64-linux =
      let
        pkgs = nixpkgs.legacyPackages.x86_64-linux;
      in
      {
        default = pkgs.mkShell {
          name = "dotnet";
          nativeBuildInputs = with pkgs; [
            dotnetCorePackages.sdk_10_0
            libmsquic
          ];

          DOTNET_BIN = "${pkgs.dotnetCorePackages.sdk_10_0}/bin/dotnet";
          DOTNET_ROOT = "${pkgs.dotnetCorePackages.sdk_10_0}/share/dotnet";
          DOTNET_DEFAULT_PROJECT_PATH = "/home/elias/Repos/DITO/OtelDemo/src/Demo.AppHost/Demo.AppHost.csproj";
        };
      };
  };
}
