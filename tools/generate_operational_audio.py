#!/usr/bin/env python3
"""Author offline warning WAVs and optional Maya clips; no synthesis runs in the game."""
import argparse
import json
import math
import struct
import wave
from pathlib import Path

LINES = {
    'welcome': "Hi, I'm Maya. Choose a layout, then start the operation. Five containers are ready in the yard. I'll guide each move aboard the ship.",
    'truck_intake': 'The next truck is arriving. The handling machine will unload its container into the yard. Wait for the machine to clear before operating the crane.',
    'handler_clear': 'The container is down. The handling machine is raising its spreader and reversing clear. Keep the transfer area open.',
    'shipment_complete': 'All five containers are aboard, and the empty spreader is parked. Your move times and incident records are saved. Reset to try another layout.'
}


def write_signal(path, duration, frequency, interval, on_time):
    rate = 22050
    with wave.open(str(path), 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(rate)
        samples = bytearray()
        for i in range(int(duration * rate)):
            t = i / rate
            phase = t % interval
            envelope = min(1, phase / .012, (on_time - phase) / .025) if phase < on_time else 0
            value = max(0, envelope) * .22 * (math.sin(2 * math.pi * frequency * t) + .2 * math.sin(2 * math.pi * frequency * 2 * t))
            samples.extend(struct.pack('<h', round(value * 32767)))
        wav.writeframes(samples)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--voice-model', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1] / 'Assets/OperationalPort'
    sound = root / 'Audio'
    sound.mkdir(parents=True, exist_ok=True)
    write_signal(sound / 'reverse.wav', 1.2, 950, 1.2, .22)
    if args.voice_model:
        from piper import PiperVoice
        voice = PiperVoice.load(str(args.voice_model))
        output = root / 'Voice'
        output.mkdir(parents=True, exist_ok=True)
        for key, line in LINES.items():
            with wave.open(str(output / (key + '.wav')), 'wb') as wav:
                voice.synthesize_wav(line, wav)
            print(key, flush=True)
        (output / 'transcript.json').write_text(json.dumps(LINES, indent=2) + '\n')
    print('Operational audio assets ready.', flush=True)


if __name__ == '__main__':
    main()
