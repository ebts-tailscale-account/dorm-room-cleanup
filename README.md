# Dorm Room Cleanup Drone

This project was developed for **ROBT414 Human Robot Interaction at Nazarbayev University**. It showcases a potential helper drone that encourages people to clean their room. The drone guides the participant to each item, explains what to do, and offers encouragement as they clean. A short questionnaire asks how participants experienced its guidance.

**Group members:** Gleb Tarkinskiy and Yevgeniy Dikun

The video demonstration and code walkthrough are submitted separately through Moodle.

## Install and run the Unity project

1. Clone or download this repository. Install **Unity Editor 6000.6.10f1** through [Unity Hub](https://unity.com/download). If you plan to build a Windows executable, make sure the Windows build target is installed in Hub.
2. In Unity Hub, select **Add** and choose the `HRI_room_drone` folder. Open it with the specified editor version. Unity imports the assets, restores the packages declared in `Packages/manifest.json`, and compiles the C# scripts automatically.
3. Open `Assets/Scenes/DormRoom.unity` in the Project window and press **Play**. To fill the Editor window, select **Play Maximized** in the Game view before pressing Play.

Unity uses the [Universal Render Pipeline](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline), the [Input System](https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/packages-all/pack-safe/com-unity-inputsystem), and [Unity UI (uGUI)](https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/packages-all/pack-core/com-unity-ugui). The package versions are recorded in `HRI_room_drone/Packages/packages-lock.json`.

### Compile a standalone Windows build

1. In Unity, open **File > Build Profiles** and switch to **Windows** with the **Intel 64-bit** architecture.
2. Confirm `Assets/Scenes/DormRoom.unity` is the enabled scene. Use **Build** and choose an output folder such as `Builds/Windows`. Unity compiles the code and creates the executable and its supporting files. [Unity Build Profiles](https://docs.unity.com/en-us/engine/6000.6/manual/building-and-publishing/build-settings/create-build-profile)
3. Launch the generated `.exe` from that folder. The project is configured to start in borderless fullscreen. Keep the `_Data` folder and other generated files beside the executable. Press **Alt+F4** to close it.

### Play a session

Enter a participant name and select **Start**. Wait for the drone's wake-up cue, press **E** to get out of bed, then use **WASD** to move and the **mouse** to look around. Walk up to the item marked with a yellow outline and press **E** to collect it or clean the marked surface. Once the drone docks, select **Continue** and complete the questionnaire.

Selecting **Finish** appends the participant name, UTC submission time, and seven answers to `questionnaire_results.csv`. Unity Editor sessions save it beside this README. A standalone Windows build saves it beside the executable.

## How Qwen3-TTS is used

We used [Qwen3-TTS](https://github.com/QwenLM/Qwen3-TTS) to generate the drone's English dialogue locally. `drone_voice/generate_room_lines.py` reads the scripted lines in `Assets/Resources/DroneLines.json` and uses the [`Qwen3-TTS-12Hz-0.6B-CustomVoice` model](https://huggingface.co/Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice) with the **Aiden** voice to render WAV files. Unity selects each line at the appropriate point in the cleanup and plays its WAV file with the matching subtitle. The clips are included in the repository, so the game runs without Python, model downloads, or a microphone.

The repository also includes a separate Windows terminal prototype. It uses [Faster Whisper](https://github.com/SYSTRAN/faster-whisper) for English speech recognition, selects a scripted reply based on the transcript, and uses Qwen3-TTS to speak that reply. This microphone prototype runs separately from Unity.

### Optional Python setup

To regenerate the clips or run the terminal prototype, install [uv](https://docs.astral.sh/uv/getting-started/installation/) for Windows. The voice tools use [Python 3.12](https://www.python.org/downloads/), [PyTorch and torchaudio](https://pytorch.org/get-started/locally/), [qwen-tts](https://pypi.org/project/qwen-tts/), [Faster Whisper](https://github.com/SYSTRAN/faster-whisper), [PyAV](https://github.com/PyAV-Org/PyAV), [python-sounddevice](https://github.com/spatialaudio/python-sounddevice), and [SoundFile](https://github.com/bastibe/python-soundfile). The dependency versions are listed in [`drone_voice/requirements.txt`](drone_voice/requirements.txt).

From the repository root in PowerShell, create the environment:

```powershell
uv venv --managed-python --python 3.12 drone_voice/.venv
```

Install PyTorch and torchaudio with one of the following commands. Use the CUDA command with a compatible NVIDIA GPU and driver. Use the CPU command otherwise.

```powershell
# NVIDIA CUDA 12.8
uv pip install --python drone_voice/.venv/Scripts/python.exe torch==2.8.0 torchaudio==2.8.0 --index-url https://download.pytorch.org/whl/cu128

# CPU only
uv pip install --python drone_voice/.venv/Scripts/python.exe torch==2.8.0 torchaudio==2.8.0 --index-url https://download.pytorch.org/whl/cpu
```

Install the remaining dependencies:

```powershell
uv pip install --python drone_voice/.venv/Scripts/python.exe -r drone_voice/requirements.txt
```

Run either of these commands from the repository root. The first use downloads the required model weights into `drone_voice/.models/` by default. For the microphone prototype, press **Enter** to record or type **q** to exit.

```powershell
# Regenerate the WAV files used by Unity
.\drone_voice\.venv\Scripts\python.exe .\drone_voice\generate_room_lines.py

# Run the separate microphone prototype
.\drone_voice\.venv\Scripts\python.exe .\drone_voice\cli.py
```

## Code organization

| Location | Responsibility |
| --- | --- |
| `HRI_room_drone/Assets/Scenes/DormRoom.unity` | Furnished room, player, drone, charger, and cleanup targets |
| `HRI_room_drone/Assets/Scripts/DormPlayerController.cs` | Wake-up, first-person movement, and mouse look |
| `HRI_room_drone/Assets/Scripts/DormDroneController.cs` | Flight and motor sound, nearest-target selection, marking, and docking |
| `HRI_room_drone/Assets/Scripts/CleanupTarget.cs` and `TrashOutline.shader` | Cleanup target type and yellow outline |
| `HRI_room_drone/Assets/Scripts/DormDroneScreen.cs` and `QuestionnaireCsv.cs` | Participant screen, subtitles, questionnaire, and CSV export |
| `HRI_room_drone/Assets/Resources/` | Scripted dialogue and generated speech clips |
| `drone_voice/` | Qwen3-TTS clip generator and terminal voice prototype |
