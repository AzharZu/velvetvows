# VelvetVows preservation notes

Prepared on 2026-11-12.

## Current remote status

At preparation time this repository has no configured GitHub remote. **Do not delete the local project until an external copy has been uploaded and independently verified.**

## What must be preserved

- `Assets/`, including every `.meta` file, `Packages/` and `ProjectSettings/`: the editable Unity project.
- Root artwork folders (`photo/`, `phto2/`, `voices/`), reports, capture folders and `Playthrough_Recording/`: original creative material, validation evidence and recordings.
- `Builds/`: standalone applications and archives. Existing builds are explicitly included in this preservation snapshot even though `.gitignore` excludes new build files by default. Future builds must also be preserved explicitly. See `docs/build-backup-manifest.json` for SHA-256 checksums.
- Git history: preserve with a verified remote or a Git bundle stored off this laptop.

`Library/`, `Temp/`, `Logs/`, generated IDE project files and `UserSettings/` are ignored local Unity data. Unity can regenerate the import cache; preserve personal editor settings separately if needed.

## Large files

The source contains `.blend` and `.glb` files over 100 MiB, and the recordings contain large `.mp4` and `.f32` files. Existing Git history also contains large binary objects. A normal GitHub push may reject these; adding Git LFS rules alone will not convert existing history. Complete a suitable LFS migration or use an external archive, preserving the original history before any rewrite.

## Verification before deleting the laptop copy

1. Confirm the intended branch and commit exist on GitHub.
2. Restore into a separate directory from the remote, including real LFS content if used; check file hashes against the source.
3. Confirm all builds and any other unique excluded files are in an off-device copy, with matching hashes.
4. Open the restored project with Unity 6000.0.84f1 and confirm the main scene and its asset references load.

These steps are a checklist, not a claim that remote backup has already completed.
