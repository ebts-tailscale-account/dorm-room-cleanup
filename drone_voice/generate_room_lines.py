import json
import os
import sys
from pathlib import Path

from voice import DroneVoice


if __name__ == "__main__":
    voice_dir = Path(__file__).resolve().parent
    resources = voice_dir.parent / "HRI_room_drone" / "Assets" / "Resources"
    output = resources / "DroneVoice"
    output.mkdir(exist_ok=True)
    os.environ.setdefault("HF_HOME", str(voice_dir / ".models"))
    # The line ID is also the WAV filename that Unity looks up at runtime.
    lines = json.loads((resources / "DroneLines.json").read_text(encoding="utf-8"))["lines"]
    if len(sys.argv) > 1:
        lines = [line for line in lines if line["id"] in sys.argv[1:]]
        if not lines:
            raise SystemExit("No matching dialogue line IDs.")
    voice = DroneVoice("auto")
    for line in lines:
        print(f"Generating {line['id']}", flush=True)
        voice.synthesize(line["text"], output / f"{line['id']}.wav")
