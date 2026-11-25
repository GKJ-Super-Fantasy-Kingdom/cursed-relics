# Contributing to Cursed Relics

## Report a bug

Include the game, mod and BepInEx versions, along with your kingdom, day and reproduction steps. For a display or input issue, include the UI scale and input device. Remove personal details and local paths from any log excerpt.

## Build and test

Use the .NET SDK, Python with Pillow, the managed assemblies from your game installation, and a BepInEx 5 profile. Set the `GameDir` and `ProfileDir` environment variables to the game and profile directories. Run `python package.py` from the repository root to build the local ZIP in `dist`.

Run `tests/VerifyBindings.ps1` in PowerShell 7 against the built package and your installed assemblies. For live tests, use a copied save. Keep game binaries, saves, personal logs, and local build output out of commits.

## Submit a change

Fork the repository, work on a branch, and describe the player-visible effect and the tests you ran. Use Conventional Commits for commit messages and pull request titles.
