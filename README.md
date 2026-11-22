# Dorm Room Cleanup with a Mini Drone

This Unity prototype explores whether a virtual robot's guidance and encouragement can motivate people to clean a dorm room. The playable scene is in `HRI_room_drone/`. It uses locally generated Qwen3-TTS dialogue clips. The separate microphone prototype runs in Windows PowerShell.

## Try the drone voice in PowerShell

From the project root, run the following. Press **Enter**, say “I'm ready,” “Where should I look?” or “I'm done,” and wait for the drone's reply. Type **q** and press **Enter** to exit. The prototype listens in English for five seconds at a time.

```powershell
.\drone_voice\.venv\Scripts\python.exe .\drone_voice\cli.py
```

If you need to set up the environment, run these commands from the project root with `uv` installed. This installs Python 3.12 into the project and uses the CUDA 12.8 build of PyTorch for the RTX 5070. The first recording downloads model weights for local English transcription and speech synthesis. The CLI defaults to writing them under `drone_voice/.models/`.

```powershell
$env:UV_PYTHON_INSTALL_DIR = Join-Path $PWD 'drone_voice\.python'
$env:UV_CACHE_DIR = Join-Path $PWD 'drone_voice\.uv-cache'
uv venv --managed-python --python 3.12 drone_voice/.venv
uv pip install --python drone_voice/.venv/Scripts/python.exe torch==2.8.0 torchaudio==2.8.0 --index-url https://download.pytorch.org/whl/cu128
uv pip install --python drone_voice/.venv/Scripts/python.exe -r drone_voice/requirements.txt
```

Whisper transcribes your voice on the CPU. The prototype selects a short reply from the transcript and voices it with Qwen3-TTS using the Aiden preset. It uses the Windows default microphone and speakers. [Faster Whisper](https://github.com/SYSTRAN/faster-whisper) · [Qwen3-TTS](https://github.com/QwenLM/Qwen3-TTS)

## Open the Unity project

1. In Unity Hub, add the `HRI_room_drone` folder as an existing project and open it with Unity `6000.6.10f1`.
2. In Unity's Project panel, open `Assets/Scenes/DormRoom.unity` to inspect the furnished room.

## Fullscreen playtest

- In the Unity Editor, select **Play Maximized** in the Game tab's play-mode dropdown, then select **Play**.
- For a standalone Windows build, open **File > Build Profiles**, choose Windows, and use `Assets/Scenes/DormRoom.unity` as the enabled scene. Set **Player > Resolution and Presentation > Fullscreen Mode** to **Fullscreen Window**. Build to a separate folder and keep the executable, its `_Data` folder, and the accompanying files together. In the standalone player, **Alt+F4** closes the game. Relaunch it for the next participant.

## Source submission

Keep the source assets and their `.meta` files, `Packages/`, `ProjectSettings/`, and the project scripts and documentation. Git ignores Unity's generated folders, Windows builds, the Python environment and model cache, and `questionnaire_results.csv`. The CSV contains participant names and is stored separately from the source submission.

## Prepare the room

The scene has two beds, two desks with laptops and chairs, wardrobes, a wooden door, a window with a daylight skybox, and an overhead light. The mini drone starts above a charging pad on the left desk. There are six movable trash items and one desk smudge to clean.

## Play the cleanup scene

Press **Play** in Unity, enter your name, and select **Start**. The screen then goes black while the drone flies to the bed and asks you to wake up and clean the room. Press **E** after its cue, then use **WASD** to move and the **mouse** to look around. The motor plays during flight. The drone flies to the cleanup target closest to it, points a beam at the target, and gives it a yellow outline. It names the target through subtitles and Qwen3-TTS voice clips.

Walk up to the highlighted target and press **E** to collect it or clean the surface. The drone congratulates you and guides you to the next target. When the room is clean, it gives a final congratulations, returns to its charging station, and powers down. Select **Continue** on the **Cleaning Complete** screen to answer the questionnaire. Selecting **Finish** appends your name, the UTC submission time, and all seven answers to `questionnaire_results.csv`. Unity Editor runs write the file beside `README.md` in the project root. A standalone Windows build writes it beside its executable.

## Questionnaire

1. How encouraged did you feel to pick up the trash after the drone pointed it out?
2. How much did you like the drone's encouragement?
3. How likely would you be to clean your real room more often if you had a robot like this?
4. How easy was it to tell which item the drone wanted you to clean up?
5. How clear were the drone's spoken instructions?
6. How comfortable did you feel with the drone flying around you?
7. What would you change about how the drone helped you?
