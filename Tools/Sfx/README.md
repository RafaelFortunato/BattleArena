# Sound effect tools

Scripts that generate the game's sound effects with Stable Audio 3 Medium on the local ComfyUI
(`C:\Users\GatO\Documentos\ComfyUI`, started with its own `python_embeded` Python on port 8188).

| Script | Run with | What it does |
|---|---|---|
| `gen_sfx.py [name ...]` | any Python | Queues prompts from its `SOUNDS` list (4 takes each) and waits. Renders land in `ComfyUI/output/audio/battlearena/<name>/`. |
| `process_sfx.py [name ...]` | `python_embeded/python.exe` | Trims silence, fades, levels to -1 dBFS and writes the best 3 takes to `Assets/Game/Audio/Sfx/<name>_N.wav`. The ambience is crossfaded into a loop. |
| `mix_crit.py` | `python_embeded/python.exe` | Builds `crit_hit_1..3` from separately generated layers (see below). |

After writing new WAVs, reimport them in Unity. Re-running **BattleArena > Build Game Content** wires whatever `<name>_*.wav` files exist into the matching `SoundEvent` (`GameContentBuilder.Audio.cs`).

## Layered sounds

For a sound that has to feel great rather than just be recognisable (crits, big hits), generate each
layer as its own prompt and mix them: a sharp transient, a heavy body, and a bright sweetener, offset
by a few milliseconds and glued with a soft clip. `mix_crit.py` is the template: copy it, change the
layer names, lengths, gains and offsets.
