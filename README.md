# TerraWeave
An experimental patching system for Terraria 1.4, and a very minimalistic modloader.
This is a project written for fun, and not meant to compete with tModLoader once it releases for 1.4.

# Overview
TerraWeave consists of three components:
- Differ: this is a program that takes a vanilla Terraria.exe and a modified one, "Diff"s them and creates a .tweave patch file
- Installer: this modifies the user's vanilla game to be able to recognize the patch files created by the Differ
- Common: this is the general library used by both Differ and Installer, and is the bulk of TerraWeave

# Building
Simply clone this repository, open the TerraWeave.sln file and you're all set!
(TerraWeave uses .NET Core 3.1 to build. Make sure you have that installed)

# Testing (important)
TerraWeave is still in very early development and not stable. In order to test it, your `bin/Debug/netcoreapp3.1` directory must contain:
- The vanilla Terraria.exe file
- Your modified terraria file, named TerrariaModified.exe (case sensitive)
- ReLogic.dll (you can grab it from tModLoader's repository)

As part of the testing process, Diff currently tries to apply the patch it creates to terraria and saves the resulting executable as PatchedTerraria.exe. This testing routine will be deprecated as TerraWeave develops.

Contributions welcome!
