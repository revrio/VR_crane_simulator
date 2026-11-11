"""Offline authoring of mode-aware Maya cues; model is not bundled in the APK."""
import argparse
import json
from pathlib import Path
import wave
from piper import PiperVoice

COMMON = {
    'welcome': "Hi, I'm Maya, your port instructor. Choose a layout on the console, then select Start operation. Follow the glowing pickup outline and the direction guide.",
    'align': "Move the gantry and trolley toward the highlighted container. The console shows how far left, right, shipward or yardward to move.",
    'lift': "You're connected. Raise the container clear of nearby stacks before travelling. Release the control to stop the winch.",
    'transfer': "Carry the container to its highlighted receiving slot. The cabin monitor can show the load, destination, or port overview.",
    'land': "You're over the receiving slot. Lower gently onto the deck or the supporting container. A little alignment tolerance is allowed.",
    'settle': "Let the released container settle on its support while I check the delivery.",
    'accepted': "Delivery accepted. Follow the console for the next task.",
    'park': "All containers are placed. Return the empty spreader to the highlighted parking position.",
    'shipment_complete': "Layout complete. Thank you. Please rate your head, neck, shoulder, hand and eye experience using the feedback sliders on the console.",
}
CONTROLS = {
    'Desktop': dict(lock='press Space', fine='hold Left Shift', lower='press the Down Arrow key', pause='click Resume or press Escape'),
    'Quest': dict(lock='press the right controller A button', fine='squeeze the right grip', lower='push the right thumbstick down', pause='point at Resume on the console and press the trigger'),
    'Simulator': dict(lock='hold Space and tap B', fine='hold Space and G', lower='hold Space and press S', pause='click Resume on the console using the simulated controller ray and trigger'),
}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('model',type=Path)
    args=parser.parse_args()
    voice=PiperVoice.load(str(args.model))
    root=Path(__file__).resolve().parents[1]/'Assets/Resources/PortVoice'
    transcript={}
    for mode, controls in [('Common',None), *CONTROLS.items()]:
        lines=COMMON if controls is None else {
            'couple': f"Ready to connect. {controls['lock'].capitalize()} to lock the highlighted container. Contact by itself does not lock it.",
            'release': f"The container is supported. {controls['lock'].capitalize()} to release it, then let it settle.",
            'lower': f"You're above the container. To lower, {controls['lower']}. For gentle movement, {controls['fine']} while operating the hoist.",
            'paused': f"The operation is paused. To continue, {controls['pause']}.",
        }
        folder=root/mode
        folder.mkdir(parents=True,exist_ok=True)
        for key,line in lines.items():
            with wave.open(str(folder/(key+'.wav')),'wb') as stream:voice.synthesize_wav(line,stream)
            transcript[mode+'/'+key]=line
            print(mode+'/'+key,flush=True)
    (root/'transcript.json').write_text(json.dumps(transcript,indent=2)+'\n')

if __name__=='__main__':main()
