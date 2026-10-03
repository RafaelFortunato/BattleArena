# Sound effect tools

Scripts that generate the game's sound effects with Stable Audio 3 Medium on the local ComfyUI
(`C:\Users\GatO\Documentos\ComfyUI`, started with its own `python_embeded` Python on port 8188).

| Script | Run with | What it does |
|---|---|---|
| `gen_sfx.py [name ...]` | any Python | Queues prompts from its `SOUNDS` list (4 takes each) and waits. Renders land in `ComfyUI/output/audio/battlearena/<name>/`. |
| `process_sfx.py [name ...]` | `python_embeded/python.exe` | Trims silence, fades, levels to -1 dBFS and writes the best 3 takes to `Assets/Game/Audio/Sfx/<name>_N.wav`. The ambience is crossfaded into a loop. |
| `mix_layers.py [sound ...]` | `python_embeded/python.exe` | Builds `<sound>_1..3` from separately generated layers (see below). Recipes: `crit_hit`, `chain_lightning`. |

After writing new WAVs, reimport them in Unity. Re-running **BattleArena > Build Game Content** wires whatever `<name>_*.wav` files exist into the matching `SoundEvent` (`GameContentBuilder.Audio.cs`).

## Layered sounds

For a sound that has to feel great rather than just be recognisable (crits, big hits), generate each
layer as its own prompt and mix them: a sharp transient, a heavy body, and a bright sweetener, offset
by a few milliseconds and glued with a soft clip. Add the layer prompts to `gen_sfx.py`, then a recipe
(layer names, lengths, gains, offsets) to `RECIPES` in `mix_layers.py`.
