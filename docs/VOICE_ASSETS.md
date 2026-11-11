# Maya voice and avatar

Current Operational Port cues are authored by `tools/generate_port_mode_voice.py` into `Assets/Resources/PortVoice`. The 21 recordings include nine common task cues and four control-specific cues for each of Desktop, Quest and Simulator. They use the same `en_US-ljspeech-high` voice and play offline. Quest recordings contain no keyboard shortcuts. The completion cue requests the post-layout feedback sliders. `transcript.json` in that resource folder contains every spoken line.

Maya is a fictional port instructor. Her stylized full-body NPC is built from original Unity geometry in `ResearchExperienceBuilder.cs` and extended/placed by `ResearchCabinBuilder.cs`; the original fixed portrait is no longer displayed. `ResearchInstructor.cs` drives greeting, head-look and pointing gestures. Head/hand/mouth movement is illustrative animation, not phoneme-aligned lip synchronization.

The 13 English speech recordings in `Assets/Research/Voice` were generated locally using Piper 1.8.0 and the `en_US-ljspeech-high` female voice. The voice's [model card](https://huggingface.co/rhasspy/piper-voices/raw/main/en/en_US/ljspeech/high/MODEL_CARD) identifies the [LJ Speech dataset](https://keithito.com/LJ-Speech-Dataset/) as public domain. The project ships generated WAVs and the original instruction transcript, not the model or Piper runtime. Speech playback requires no network access.

Authoring script: `tools/generate_guide_voice.py`. In a separate Python environment install `piper-tts`, download the model and matching `.onnx.json` from the model repository, and run:

```sh
python tools/generate_guide_voice.py /path/to/ljspeech.onnx
```

Finish generation before starting Unity asset import/build. All clips should be regenerated together when changing voices. Unity imports the resulting recordings normally. `transcript.json` is the exact script; captions are shorter, contextual instructions including live job IDs.

Maya's speech is deterministic and tied to simulator state. H/repeat repeats the current instruction, M/voice toggles playback, and captions remain visible. Voice changes during a run are logged. Use the same guidance policy in both experimental conditions.
# Operational iteration-one reuse

The operational scene reuses the existing female welcome, lower, couple, lift, land, release, settle and pause recordings for action coaching. Truck/reachstacker stages have new contextual captions and machinery audio. The old three-delivery completion recording is intentionally not used for the one-container operational shipment. No new external character or voice assets were downloaded for this iteration; the original procedural Maya geometry is reused.


# Operational iteration-three speech and signals

Eight additional recordings in `Assets/OperationalPort/Voice` use Piper **1.8.0** and the same `en_US-ljspeech-high` female model. The [model card](https://huggingface.co/rhasspy/piper-voices/raw/main/en/en_US/ljspeech/high/MODEL_CARD), checked on 5 November 2026, identifies the source dataset as public domain. Only generated WAVs and `transcript.json` ship; no model or TTS runtime is bundled. `tools/generate_operational_audio.py --voice-model /path/to/en_US-ljspeech-high.onnx` reproduces these lines.

New lines cover operational welcome, crossing wait, protected stop, worker clearance, readiness to resume, truck intake, reachstacker clearance and five-container completion. Existing generic action clips are retained. Machine/voice state selects clips offline; no external speech service runs in the lab. The same authoring script creates the original reversing and alarm tone WAVs in `Assets/OperationalPort/Audio` using bounded sine signals. Contact sounds remain from the original repository.
