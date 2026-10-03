"""Queue BattleArena sound effects on the local ComfyUI (Stable Audio 3 Medium, direct prompts).

    python gen_sfx.py            # queue every sound, wait for all
    python gen_sfx.py name ...   # only these sounds
Outputs land in ComfyUI/output/audio/battlearena/<name>/<name>_0000N_.wav (4 takes each).
"""
import json
import random
import sys
import time
import urllib.request

HOST = "http://127.0.0.1:8188"
TAIL = ", close mic, dry room, isolated one-shot sound effect, no music, no voice"
NEG = "music, melody, speech, vocals, singing, hiss, clipping, distortion"

# name, seconds generated, prompt
SOUNDS = [
    ("bolt_shoot", 1.5, "small magic energy bolt fired from a wizard staff, soft airy whoosh with a light sparkling zap, short and quiet, cartoon fantasy game"),
    ("bolt_hit", 1.5, "small magic spell touching a monster, soft gentle magical shimmer, a short bright twinkling sparkle chime that quickly fades, smooth and clean, fantasy game spell impact"),
    ("arcane_lance_cast", 2.0, "powerful arcane lance spell launched, bright piercing magical beam whoosh with a crystalline shimmering ring, fast attack, fantasy game spell"),
    ("arcane_hit", 1.5, "arcane magic projectile impact, crisp crystal shatter with a glassy magical sparkle burst, short, fantasy game impact"),
    ("arcane_orbit_cast", 2.5, "magic orbs summoned to circle a wizard, rising shimmering chime swirl with soft humming energy and a sparkling magical whoosh, fantasy game spell"),
    ("fireball_cast", 2.0, "fireball spell cast, quick roaring flame burst whoosh launched forward with crackling fire, fantasy game spell"),
    ("fireball_explosion", 2.5, "fireball explosion impact, punchy fiery boom with crackling flames and small debris, medium size explosion, fantasy game"),
    ("chain_lightning", 2.0, "chain lightning spell, sharp electric zap and crackling thunder snap jumping between targets, buzzing electricity, fantasy game spell"),
    ("frost_nova", 2.5, "frost nova spell, icy blast bursting outward, crackling ice crystals forming and shattering, cold wind whoosh, fantasy game spell"),
    ("meteor_fall", 2.5, "flaming meteor falling from the sky, descending fiery whoosh with a rumbling roar getting louder, fantasy game"),
    ("meteor_impact", 3.0, "meteor crashing into the ground, heavy deep explosion boom with rocks and earth debris and a rumbling ground shake, fantasy game"),
    ("phase_step", 1.5, "magical teleport blink, quick airy whoosh with a reversed shimmering zap and a soft pop, fantasy game spell"),
    ("toxic_miasma", 3.0, "toxic poison cloud spell released, bubbling acidic hiss with a gassy gurgling whoosh, sickly fog spreading, fantasy game spell"),
    ("enemy_hit", 1.5, "monster being hit by magic, short soft fleshy thump with a light slap, punchy but quiet, cartoon fantasy game hit"),
    ("crit_hit", 1.5, "very satisfying critical hit in a fantasy action game, a crisp punchy magical pop layered with a bright high pitched crystal ching chime that rings out with sparkles, rewarding and juicy, clean and short"),
    # Critical hit, built from layers mixed by mix_crit.py: transient, body, sweetener.
    ("crit_layer_attack", 1.5, "single very short sharp sword slash snap, bright cutting whip crack transient, extremely fast attack, fantasy game"),
    ("crit_layer_body", 1.5, "single deep punchy heavy impact thump with a tight sub bass boom, short and powerful, video game hit"),
    ("crit_layer_sweet", 2.0, "single bright high pitched metallic ching, a struck crystal bell ringing out with a sparkling shimmering tail, clean and pure, fantasy game"),
    ("enemy_death", 1.5, "small monster defeated, soft magical poof burst with a puff of smoke and a tiny squelch, cartoon fantasy game"),
    ("player_hurt", 1.5, "hero taking damage, dull heavy body impact thud with a short low whoosh, video game damage sound"),
    ("player_death", 3.5, "hero defeated, heavy dramatic low boom impact with a deep descending whoosh fading out, dark game over hit"),
    ("gem_pickup", 1.5, "small experience gem collected, bright short crystal chime blip, high pitched sparkling ding, cheerful video game pickup"),
    ("level_up", 3.0, "level up, magical rising sparkling chime arpeggio with a bright shimmering whoosh, triumphant fantasy game power up"),
    ("ui_click", 1.5, "soft user interface button click, short wooden tap with a light pop, clean menu sound"),
    ("ui_hover", 1.5, "very soft subtle user interface hover tick, tiny light tap, clean menu sound"),
    ("upgrade_pick", 2.0, "upgrade card selected, magical confirm sound, bright shimmering sparkle with a soft whoosh, fantasy game menu"),
    ("chest_open", 2.5, "old wooden treasure chest opening, creaking hinge and lid thump followed by jingling gold coins and a magical sparkle, fantasy game"),
    ("chest_appear", 2.0, "magical sparkle poof as an object appears out of thin air, bright shimmering twinkle burst, fantasy game"),
    ("well_heal", 3.0, "magical healing water, splash of water from a stone well with a soft rising healing chime shimmer, fantasy game"),
    ("shard_pickup", 1.5, "magical crystal shard collected, bright resonant crystal ring with a sparkling shimmer, fantasy game pickup"),
    ("mission_start", 2.5, "quest started alert, deep fantasy war horn blast, short heroic call"),
    ("mission_complete", 3.0, "quest completed, triumphant bright magical chime flourish with sparkles, short fantasy game success jingle"),
    ("mission_failed", 2.5, "quest failed, low descending dark magical tone with a soft thud, short fantasy game failure sting"),
    ("boss_roar", 4.0, "giant undead ghost skull monster roaring, deep echoing ghostly growl with a dark rumble, fantasy boss appears"),
    ("arrow_shoot", 1.5, "bow string twang releasing an arrow followed by a quick whoosh of the arrow flying"),
    ("arrow_hit", 1.5, "arrow hitting a target, short sharp wooden thunk impact"),
    ("enemy_magic_shoot", 1.5, "small dark magic orb fired by a monster, quick wobbly magical whoosh with a ghostly zap, fantasy game"),
    ("enemy_magic_hit", 1.5, "dark magic orb impact, soft muffled magical burst with a ghostly crackle, fantasy game"),
    ("bomb_throw", 1.5, "small bomb tossed, short fizzing lit fuse with an airy throw whoosh, cartoon fantasy game"),
    ("bomb_explosion", 2.5, "small bomb explosion, punchy cartoon boom with crackle and debris, fantasy game"),
    ("ground_slam", 2.5, "giant club smashing the ground, heavy deep thud impact with cracking stone and dirt debris, fantasy game"),
    ("enemy_windup", 1.5, "monster charging a dark magic attack, short rising ominous energy swell with crackling, fantasy game"),
    ("curse_cast", 2.0, "dark curse spell cast on the ground, eerie whispery ghostly swell with a deep magical hum, fantasy game"),
    ("curse_eruption", 2.0, "dark magic eruption from the ground, sudden burst of shadowy energy with a deep whoosh and crackle, fantasy game"),
    ("dash_charge", 2.0, "wild beast charging, heavy snort followed by fast stomping footsteps and a rushing whoosh, fantasy game"),
    ("victory", 5.0, "victory fanfare, short triumphant orchestral brass and chimes flourish, fantasy game win jingle, no vocals"),
    ("defeat", 5.0, "defeat sting, short sad descending orchestral strings with a low drum hit, fantasy game over jingle, no vocals"),
    ("ambience_forest", 47.0, "peaceful forest ambience, gentle wind through autumn trees, distant birds chirping, rustling leaves, calm natural outdoor atmosphere, continuous background, no music, no voice"),
]

# Per-sound additions to the negative prompt, for takes that drifted toward the wrong kind of sound.
EXTRA_NEGATIVE = {"crit_hit": ", gunshot, bullet, firearm, crunch, low rumble, muddy", "bolt_hit": ", gunshot, bullet, firearm, ricochet, metal impact, crackle, electricity, static, fizz, noise burst"}

# Jingles keep their music; everything else gets the one-shot tail.
MUSICAL = {"victory", "defeat", "level_up", "mission_complete", "mission_failed"}


def workflow(name, seconds, prompt, takes):
    text = prompt if name in MUSICAL or name.startswith("ambience") else prompt + TAIL
    negative = "speech, vocals, singing, hiss, clipping, distortion" if name in MUSICAL else NEG
    negative += EXTRA_NEGATIVE.get(name, "")
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "stable_audio_3_medium.safetensors"}},
        "2": {"class_type": "CLIPLoader", "inputs": {"clip_name": "t5gemma_b_b_ul2.safetensors", "type": "stable_audio", "device": "default"}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"text": text, "clip": ["2", 0]}},
        "4": {"class_type": "CLIPTextEncode", "inputs": {"text": negative, "clip": ["2", 0]}},
        "5": {"class_type": "EmptyLatentAudio", "inputs": {"seconds": seconds, "batch_size": takes}},
        "6": {"class_type": "KSampler", "inputs": {"seed": random.randint(0, 2**31), "steps": 8, "cfg": 1.0, "sampler_name": "lcm",
                                                    "scheduler": "simple", "denoise": 1.0, "model": ["1", 0], "positive": ["3", 0],
                                                    "negative": ["4", 0], "latent_image": ["5", 0]}},
        "7": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["6", 0], "vae": ["1", 2]}},
        "8": {"class_type": "SaveAudioWAV", "inputs": {"audio": ["7", 0], "filename_prefix": f"audio/battlearena/{name}/{name}", "bit_depth": "16"}},
    }


def post(path, payload):
    request = urllib.request.Request(HOST + path, data=json.dumps(payload).encode(), headers={"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(request))


def get(path):
    return json.load(urllib.request.urlopen(HOST + path))


def main():
    only = set(sys.argv[1:])
    pending = {}
    for name, seconds, prompt in SOUNDS:
        if only and name not in only:
            continue
        takes = 2 if name.startswith("ambience") else 4
        pending[post("/prompt", {"prompt": workflow(name, seconds, prompt, takes)})["prompt_id"]] = name
    print(f"queued {len(pending)}", flush=True)

    while pending:
        time.sleep(3)
        for prompt_id in list(pending):
            history = get(f"/history/{prompt_id}")
            if prompt_id not in history:
                continue
            status = history[prompt_id].get("status", {})
            print(pending.pop(prompt_id), status.get("status_str"), flush=True)


if __name__ == "__main__":
    main()
