# GitHub backup verification

Verified on 2026-11-12 for the private repository [AzharZu/velvetvows](https://github.com/AzharZu/velvetvows).

- Remote `main` matched local commit `517d1aac79d7979cca1797d8412ecc481ec02b61` before this report was added.
- All 8,187 paths from original preservation commit `b7a3fa0` survived LFS migration. The 46 migrated file paths were checked against the original Git blob hashes and their full local SHA-256 LFS hashes; contents matched.
- `git lfs fsck` passed.
- GitHub LFS confirmed download availability for all 38 unique large objects, totaling 6,336,978,236 bytes (approximately 6.3 GB). The first 1 MiB of every remote object was downloaded and matched against its local original. Complete remote LFS files were not all downloaded again.
- An independent bare clone with `--filter=blob:none` matched the local commit and complete Git tree. Its connectivity check passed.
- README, the main Unity scene, the diary image and the dance screenshot were independently fetched through the verification clone and compared byte for byte with their committed originals.
- The source snapshot includes all 1,161 build files listed in [the build manifest](build-backup-manifest.json), along with project source, assets, captures and recordings. The manifest records full SHA-256 hashes for those build files.

Unity import caches, temporary data, logs, generated IDE projects and personal editor settings remain excluded as described in [BACKUP.md](../BACKUP.md). A new full Unity import and gameplay run from the remote copy were not performed during this backup verification.

Restore with Git LFS installed: clone the repository, run `git lfs pull`, then open it with Unity 6000.0.84f1. Keep the repository private while it contains third-party assets with redistribution restrictions.
