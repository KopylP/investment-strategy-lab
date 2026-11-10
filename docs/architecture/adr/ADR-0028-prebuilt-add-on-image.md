# ADR-0028: The add-on is installed from the repository as a prebuilt image

- Status: accepted (2026-11-10)

## Context

The add-on was a local one: `deploy.sh` built the artifacts on a laptop, copied the folder to Home Assistant over SSH, and the
Supervisor built the Docker image on the Raspberry Pi 5 (5–10 minutes, every update). An update needed the laptop, the SSH
add-on and a reachable Home Assistant; nothing happened after a merge until someone ran the script.

## Decision

- **GitHub Actions builds the image and Home Assistant only pulls it.** `config.yaml` names `image: ghcr.io/kopylp/investlab`;
  the Supervisor pulls `<image>:<version>` instead of building. The workflow `addon-release.yml` runs on a native arm64 runner
  on every push to `main` that changes what goes into the add-on.
- **Only aarch64.** The add-on runs on one Raspberry Pi 5; an amd64 image would be built for nobody. Adding it is one more
  platform in the workflow and one more `arch` in `config.yaml`.
- **The image is published before the version.** The version in `config.yaml` is what Home Assistant offers, so the workflow
  pushes the image first and commits the new version after it. It decides the version by the registry: the image of the
  current version exists → the next patch; it does not → that version as it is (set by hand, or the first release).
- **Only the two newest images are kept.** After a successful release the workflow deletes every older version of the
  package; the previous one stays so an installation that has not updated yet can still reinstall it. Each version is one
  plain manifest (no provenance or SBOM index), so deleting a version never takes part of a kept image with it.
- **The repository is the add-on store.** `repository.yaml` is in the root; Home Assistant follows the repository and offers
  each new version, or installs it itself with auto-update. No SSH, VPN or secret on GitHub reaches Home Assistant.
- **`deploy.sh` stays for changes not on `main`**: it installs the working copy as the local add-on, built on the device, by
  dropping the `image:` line from the copy.

## Consequences

- The add-on from the store has another slug than the local one (`<repository hash>_investlab` instead of `local_investlab`),
  so it has its own `/data`. Moving from the local add-on is done once with the export/import markers in `/share/investlab`
  (`run.sh`, DOCS.md); `share` is mapped for that.
- The image package in GitHub Container Registry must be public, or Home Assistant needs registry credentials.
- A version is visible in Home Assistant only after the image build (about 10 minutes after a merge).
