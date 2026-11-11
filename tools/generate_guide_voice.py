"""Offline authoring only: pip install piper-tts; provide a Piper model path.
Generated recordings are bundled in Unity; the model/runtime are not shipped.
"""
import json, pathlib, sys, wave
from piper import PiperVoice
LINES = {
'welcome': "Hi, I'm Maya, your port instructor. Click Practice to begin. I'll guide you through each delivery. Use your controllers, keyboard, or the on screen controls.",
'align': "Let's pick up the glowing container. Rotate the crane and move the trolley. Line up the white ring under your hook with the container. The alignment guide shows which way to move.",
'lower': "You're above the container. Use the lower control to bring the hook down. Watch the height guide. Use fine control for slower, precise movement.",
'couple': "Ready to connect! Press space, or the right controller A button, to lock onto the container.",
'lift': "Good, you're connected. Raise the hook to lift the container clear of the deck before moving across the yard.",
'transfer': "Now carry your container to the glowing matching bay. Follow the route on the ground. Use the ground ring to line up your landing.",
'land': "You're over the receiving bay. Lower gently. Use fine control near the ground. Keep the whole container inside the marked area.",
'release': "The container is on the deck. Press space, or right A, to release it. Then let it settle.",
'settle': "That's looking good. Let the container stay still for a moment so I can confirm the delivery.",
'return': "Delivery accepted! Return the empty hook to the turquoise home zone on the quay. Match the floating beacon's height. The next job starts when you park there.",
'paused': "We're paused. Click Resume, or press Escape, when you're ready. Paused time is excluded from your results.",
'complete': "Great work! All three deliveries are complete. Save your comfort ratings. Then try the other layout and compare your work element times.",
'stopped': "The run has stopped, and your progress was saved. Press P to begin a new practice run."
}
if __name__ == '__main__':
    voice = PiperVoice.load(sys.argv[1])
    output = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else pathlib.Path(__file__).resolve().parents[1] / 'Assets/Research/Voice'
    output.mkdir(parents=True, exist_ok=True)
    for key, text in LINES.items():
        with wave.open(str(output / (key + '.wav')), 'wb') as wav:
            voice.synthesize_wav(text, wav)
        print(key, flush=True)
    (output / 'transcript.json').write_text(json.dumps(LINES, indent=2) + '\n')
