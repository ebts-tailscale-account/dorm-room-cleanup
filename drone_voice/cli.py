import argparse
import json
import os
import sys
from contextlib import redirect_stdout
from pathlib import Path
from tempfile import TemporaryDirectory

from voice import DroneVoice


def microphone_loop(voice: DroneVoice, seconds: int) -> None:
    import sounddevice as sd
    import soundfile as sf

    sample_rate = int(sd.query_devices(kind="input")["default_samplerate"])
    # Keep microphone recordings and replies in a temporary folder for this session.
    with TemporaryDirectory() as directory:
        input_wav = Path(directory) / "player.wav"
        output_wav = Path(directory) / "drone.wav"
        while input("Press Enter to speak or type q to quit: ").strip().lower() != "q":
            print(f"Listening for {seconds} seconds. Speak now.", flush=True)
            samples = sd.rec(seconds * sample_rate, sample_rate, channels=1, dtype="int16")
            sd.wait()
            sf.write(input_wav, samples, sample_rate)
            print("Preparing the drone's reply...", flush=True)
            result = voice.respond(input_wav, output_wav)
            print(f"You: {result['transcript'] or '(no speech detected)'}")
            print(f"Drone: {result['reply']}")
            samples, sample_rate_out = sf.read(output_wav, dtype="float32")
            sd.play(samples, sample_rate_out)
            sd.wait()


def main() -> int:
    parser = argparse.ArgumentParser(description="Local voice for the dorm room drone.")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--input", type=Path, help="Respond to speech in a WAV file.")
    mode.add_argument("--say", help="Speak this text directly.")
    parser.add_argument("--output", type=Path, help="WAV file to write in file mode.")
    parser.add_argument("--device", choices=("auto", "cuda", "cpu"), default="auto")
    parser.add_argument("--seconds", type=int, default=5, help="Microphone recording length.")
    args = parser.parse_args()

    file_mode = args.input is not None or args.say is not None
    if file_mode != (args.output is not None):
        parser.error("--output is required with --input or --say")
    if args.input is not None:
        if not args.input.is_file():
            parser.error("--input must be an existing WAV file")
        if args.input.resolve() == args.output.resolve():
            parser.error("--input and --output must be different files")
    if args.say is not None and not args.say.strip():
        parser.error("--say requires nonempty text")
    if args.seconds < 1:
        parser.error("--seconds must be positive")

    os.environ.setdefault("HF_HOME", str(Path(__file__).resolve().parent / ".models"))
    voice = DroneVoice(args.device)
    try:
        if not file_mode:
            microphone_loop(voice, args.seconds)
            return 0
        # Keep stdout reserved for JSON so another program can read the result.
        with redirect_stdout(sys.stderr):
            if args.input is not None:
                result = voice.respond(args.input, args.output)
            else:
                voice.synthesize(args.say, args.output)
                result = {"reply": args.say}
        print(json.dumps(result))
        return 0
    except (EOFError, KeyboardInterrupt):
        return 0 if not file_mode else 130
    except Exception as exc:
        print(f"Voice error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
