# Agent handoff

Read `PLAN.txt` for the original project brief and `README.md` for the user-facing overview. The Unity project is `HRI_room_drone/`. `ProjectSettings/ProjectVersion.txt` specifies Unity `6000.6.10f1`. Use the Unity CLI with the project's Pipeline package on Windows PowerShell.

Follow the workspace instructions and keep implementation simple. If a file permission or access error prevents work, stop and tell the user what needs to be fixed. Check the existing scene and components before adding new ones.

## Assets and scene setup

- `Assets/Scenes/DormRoom.unity` is the furnished shared room. It uses prefabs from the project and a room shell with a window and overhead light. The sink has been deferred. The builder is `AgentScripts/BuildDormRoom.cs` and creates the scene only when its destination is free.
- `DormRoom` has a wooden entry door, a mini drone above the left desk, and a charging pad with a child named `Docking Point`. The drone uses the same prefab as the one in `SampleScene`. Its demo flight scripts are disabled in the dorm room.
- The `Trash` hierarchy contains six collectible props and a desk smudge. Each target root has the `Trash` tag. Add the optional `CleanupTarget` component to override a display name or set `Surface Dirt` for a target that should be cleared in place.

## Gameplay sequence

The player and drone controllers are installed in `DormRoom`. The player enters a name before the drone begins its flight. The `Player Start` hierarchy contains the bed and standing positions. The drone enables wake-up on arrival, and `DormPlayerController.IsAwake` reports when the player has pressed `E` and left the bed.

1. Start with the player asleep and movement disabled. Fly the drone to the player and prompt them to wake up and clean the room. Pressing `E` wakes the player and enables `WASD` movement and mouse look.
2. Select the closest remaining cleanup target to the drone's current position. Fly near it, show a visible beam from the drone and a yellow outline on the target, then name the target and ask the player to pick it up or clean it.
3. When the player is within interaction range of the marked target, `E` collects movable trash or clears surface dirt. Clear the highlight and beam, congratulate the player, and encourage them to keep going if targets remain. Reevaluate which target is closest after each cleanup.
4. After the last target, play the final congratulations, fly to the charging station, and power down. Show **Cleaning Complete**, then show the questionnaire from `README.md`.

## Implementation and verification

- `DormDroneController` handles the room sequence. The motor loops the supplied `Assets/Audio/drone_sfx.mp3` during flight. Dialogue text lives in `Assets/Resources/DroneLines.json`, with matching WAV clips in `Assets/Resources/DroneVoice/`. Run `drone_voice/generate_room_lines.py` with the existing voice environment to regenerate the clips. A line ID argument regenerates only that line.
- `DormDroneScreen` collects the participant name and shows dialogue, the completion screen, and the seven-question form. `QuestionnaireCsv` appends one row on successful submission to `questionnaire_results.csv` in the workspace root for Editor runs or beside the standalone executable.
- The Windows voice prototype is in `drone_voice/`. `voice.py` handles English transcription, rule-based replies, and local Qwen3-TTS synthesis. `cli.py` handles microphone capture and playback and exposes the file interface for future Unity integration. The prototype keeps models loaded between microphone turns.
- Unity can record a player clip, write it to a WAV file, and call `python drone_voice/cli.py --input player.wav --output drone.wav` with the voice environment's Python. The CLI returns a JSON object with `transcript` and `reply` on stdout and writes a PCM 16-bit WAV for playback. Use `--say "Nice work!" --output drone.wav` for a line supplied by the game. Check the process exit code before reading the WAV and use stderr for diagnostics. Use the returned `reply` for on-screen text.
- Play through the sequence with both cleanup types. Place targets so that the closest one to the drone differs from the closest one to the player. Check that movement is locked until wake-up, `E` only cleans the marked target when nearby, each cleanup advances once, and docking leads to the completion screen and questionnaire.
