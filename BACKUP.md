# VelvetVows preservation notes

Prepared on 2026-11-12.

## Current remote status

GitHub remote: `https://github.com/AzharZu/velvetvows.git` (private repository, verified through the GitHub API). **Do not delete the local project until the remote commit and LFS objects have been uploaded and independently verified.**

## What must be preserved

- `Assets/`, including every `.meta` file, `Packages/` and `ProjectSettings/`: the editable Unity project.
- Root artwork folders (`photo/`, `phto2/`, `voices/`), reports, capture folders and `Playthrough_Recording/`: original creative material, validation evidence and recordings.
- `Builds/`: standalone applications and archives. Existing builds are explicitly included in this preservation snapshot even though `.gitignore` excludes new build files by default. Future builds must also be preserved explicitly. See `docs/build-backup-manifest.json` for SHA-256 checksums.
- Git history: preserve with a verified remote or a Git bundle stored off this laptop.

`Library/`, `Temp/`, `Logs/`, generated IDE project files and `UserSettings/` are ignored local Unity data. Unity can regenerate the import cache; preserve personal editor settings separately if needed.

## Large files

Files larger than 50 MB have been migrated to Git LFS in the published branch, including existing history. Install Git LFS before restoring the project and run `git lfs pull` after cloning. `.gitattributes` records the tracked paths.

The original pre-migration snapshot is commit `b7a3fa0`, retained locally as `backup/pre-lfs-2026-11-12`. This original branch is not intended for a normal GitHub push because it contains oversized ordinary Git blobs. The migrated branch preserves the file contents with new commit hashes.

## Verification before deleting the laptop copy

1. Confirm the intended branch and commit exist on GitHub.
2. Restore into a separate directory from the remote, including real LFS content if used; check file hashes against the source.
3. Confirm all builds and any other unique excluded files are in an off-device copy, with matching hashes.
4. Open the restored project with Unity 6000.0.84f1 and confirm the main scene and its asset references load.

These steps are a checklist, not a claim that remote backup has already completed.
