import re
from pathlib import Path


QWEN_MODEL = "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice"


def reply_to(transcript: str) -> str:
    words = set(re.findall(r"[a-z]+", transcript.lower()))
    if not transcript.strip():
        return "I didn't hear you. Could you try again?"
    if words & {"wait", "later", "tired"} or re.search(
        r"\bnot\s+(?:ready|done|finished)\b", transcript, re.IGNORECASE
    ):
        return "Take your time. Tell me when you're ready."
    if words & {"done", "finished", "cleaned", "collected"}:
        return "Nice work! Keep it up."
    if words & {"help", "where", "next"}:
        return "Look for the trash with the yellow outline. Walk up to it and press E."
    if words & {"ready", "awake", "start"}:
        return "Let's get started. Look for the item I highlight."
    return "You can tell me you're ready, ask for help, or say when you're done."


class DroneVoice:
    def __init__(self, device: str):
        self.device = device
        self.recognizer = None
        self.speaker = None

    def transcribe(self, wav: str | Path) -> str:
        if self.recognizer is None:
            from faster_whisper import WhisperModel

            self.recognizer = WhisperModel("base.en", device="cpu", compute_type="int8")
        segments, _ = self.recognizer.transcribe(
            str(wav), language="en", beam_size=1, vad_filter=True
        )
        return " ".join(segment.text.strip() for segment in segments).strip()

    def synthesize(self, text: str, wav: str | Path) -> None:
        import soundfile as sf

        if self.speaker is None:
            import torch
            from qwen_tts import Qwen3TTSModel

            device = self.device
            if device == "auto":
                device = "cuda" if torch.cuda.is_available() else "cpu"
            self.speaker = Qwen3TTSModel.from_pretrained(
                QWEN_MODEL,
                device_map=device,
                dtype=torch.bfloat16 if device == "cuda" else torch.float32,
                attn_implementation="sdpa",
            )
        samples, sample_rate = self.speaker.generate_custom_voice(
            text=text, language="English", speaker="Aiden", do_sample=False
        )
        sf.write(wav, samples[0], sample_rate, format="WAV", subtype="PCM_16")

    def respond(self, input_wav: str | Path, output_wav: str | Path) -> dict[str, str]:
        transcript = self.transcribe(input_wav)
        reply = reply_to(transcript)
        self.synthesize(reply, output_wav)
        return {"transcript": transcript, "reply": reply}
