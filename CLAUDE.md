# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a BepInEx plugin for "Sultan's Game" that provides better joystick keymapping functionality. The plugin is built for .NET 6.0 targeting IL2CPP Unity games.

## Build System

- **Framework**: .NET 6.0
- **Project Type**: BepInEx plugin for IL2CPP Unity
- **Build Command**: `dotnet build`
- **Restore Dependencies**: `dotnet restore`
- **Clean Build**: `dotnet clean`

## Project Structure

- **Plugin.cs**: Main plugin entry point inheriting from BasePlugin
- **SultanFeelsGood.BetterJoystick.csproj**: Project configuration
- **Dependencies**: BepInEx framework and Sultan-sGame.GameLibs (v1.0.3)

## Key Dependencies

- **BepInEx.Unity.IL2CPP**: Plugin framework for IL2CPP Unity games
- **BepInEx.PluginInfoProps**: Plugin metadata handling
- **Sultan-sGame.GameLibs**: Game-specific library version 1.0.3

## NuGet Sources

The project uses additional NuGet sources:
- https://api.nuget.org/v3/index.json
- https://nuget.bepinex.dev/v3/index.json
- https://nuget.samboy.dev/v3/index.json

## Development Notes

- Plugin uses unsafe code blocks (AllowUnsafeBlocks=true)
- Targets IL2CPP runtime, not standard Mono
- Plugin GUID: SultanFeelsGood.BetterJoystick
- Plugin Name: A Better Joystick Keymap for Sultan's Game
- Version: 0.0.1

## Current Implementation

The plugin is in early development stage. The Load() method creates instances of SROptions.Current, CardPop, and OperationContext, then calls Do() on CardPop with an OperationContext parameter. The exact functionality of these classes is not yet implemented in the current codebase.

## Build Output

Build artifacts are placed in:
- `bin/Debug/net6.0/` - Debug builds
- `obj/Debug/net6.0/` - Intermediate build files