# LocalGames

LocalGames is a self-hosted solution for games. Load game files onto the server, then download and play them through the client app.

The client app has first class support for Linux, with included support for Umu-launcher and Proton to run Windows games on Linux.

Only a Linux build of the client app is available, and requires users to install <a href="https://github.com/Open-Wine-Components/umu-launcher">Umu launcher</a>

⚠️ This is a work in progress hobby project. It is usable but far from a 1.0 release ⚠️

# Features

Current:
  - Uploading and downloading games through the client app
  - Automatic metadata fetching through IGDB
  - Manual metadata editing
  - Automatic cloud save management - Client app finds game save files and uploads to server
  - Playtime tracking
  - Launching Windows games on Linux with Proton and Umu launcher
  - Ability to configure Proton through environmnet variables

Planned:
  - Windows build
  - Docker image and Docker Compose template
  - Mod management
  - Better project name
