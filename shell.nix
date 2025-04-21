{ pkgs ? import <nixpkgs> {} }:

let
  unstable = import (builtins.fetchTarball {
    url = "https://github.com/NixOS/nixpkgs/archive/nixos-unstable.tar.gz";
  }) { };
in
  (pkgs.buildFHSEnv {
    name = "simple-x11-env";
    targetPkgs = pkgs: (with pkgs; [
      dotnet-sdk_8
      jetbrains.rider
      fish
      udev
      alsa-lib
      fontconfig
      glew
    ]) ++ (with pkgs.xorg; [
      # Avalonia UI
      libX11
      libICE
      libSM
      libXi
      libXcursor
      libXext
      libXrandr
    ]) ++ (with unstable; [
      umu-launcher
    ]);
    runScript = "fish";
  }).env
