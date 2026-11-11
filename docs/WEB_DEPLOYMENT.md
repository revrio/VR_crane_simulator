# Browser deployment

Live private site: [PortLab Operations](https://portlab-crane-operations.superfrog4097.chatgpt.site). Sign in with the owning ChatGPT account.

The browser version builds `Assets/Scenes/OperationalPort.unity` with Unity 6000.0.26f1 and WebGL Build Support. It uses the desktop keyboard/mouse controls. Native Quest/OpenXR builds remain available separately.

The `DownloadFirst` WebGL template downloads all four runtime payloads, validates each length and SHA-256 digest, and decompresses them before enabling **Start simulator**. Unity's loader is evaluated only after this barrier and receives local Blob URLs for its framework, WebAssembly, and scene data. Downloads therefore finish before the engine starts. An interrupted, missing, or corrupt download leaves the engine stopped and offers Retry. Verified files are cached when browser storage is available.

The build packager rejects nonempty `StreamingAssets` directories: new runtime-streamed resources must be embedded in the Unity data file, or explicitly given a complete local caching solution before publishing.

## Build and package

Use **VR Research Demo > Build Operational Web** in Unity, or invoke `PortWebBuild.Build` in batch mode. The default output is `Builds/WebGL`; `PORTLAB_WEB_OUTPUT` overrides the output directory. Stop any editor using the same project before a batch build, or use a separate source copy.

From this Unity project, package a completed export with:

```sh
python3 tools/prepare_web_build.py /absolute/path/to/web/export
```

Packaging replaces the uncompressed runtime files with compressed, content-addressed payloads and writes `asset-manifest.json`. Run this once after each fresh Unity export.

The hosted static checkout is the adjacent `/home/revrio/vr/portlab-web` directory, with its output in `dist/`. Its `.openai/hosting.json` retains the Sites project identity; reuse that project for future publication.

## Validation

```sh
node tools/test_web_preloader.mjs
```

The tests check the full-download barrier, missing/truncated payloads, corrupt cache entries, cache reuse, and storage-quota failures. The browser check should download everything, stop the local server, and then start the simulator to verify that startup needs no more asset requests.

Verified on 9 November 2026: Unity WebGL build succeeded; all four loader tests passed; the 27.9 MiB download completed; the local asset server was stopped before startup; the simulator rendered and the on-screen crane controls moved the gantry without further asset downloads. No browser runtime errors were reported. Sites confirmed successful private publication.

The browser's **Download operational CSV files** button exports the current session as a ZIP. It replaces the native player's folder-opening action.
