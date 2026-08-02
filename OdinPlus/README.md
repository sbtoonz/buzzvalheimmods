# OdinPlus - Living Villages for Valheim

Transform Valheim into a living world with walking NPCs, automated builders, factions, and blueprint-based construction!

## What This Mod Adds

### 🏘️ Living Villages
Find villages scattered across the world with NPCs that walk around and perform daily tasks:

- **👨‍🌾 Farmer** - Asks for berries and mushrooms to feed the village. Give them food to earn reputation!
- **📜 Messenger** - Gives you delivery quests to help nearby workers
- **🔨 Builder** - Constructs buildings automatically when you give them wood and stone
- **⛪ Priest** - Trains your skills (Swords, Axes, Bows, etc.) in exchange for gold coins
- **⚔️ Guards** - Patrol the village perimeter to protect against monsters

All NPCs walk around naturally - no more standing still! They'll wander near their posts while you explore.

### 🏗️ Blueprint System - Build Once, Copy Forever

**Visual Selection (Easy Method):**

1. Build a structure anywhere in the world
2. Open console with `F5`
3. Type `/selectblueprint MyHouse` and press Enter
4. Click on two opposite corners of your building (green and red markers appear)
5. Use your **mouse scroll wheel** to adjust the height until the blue box covers your whole building
6. Click once more to save

Your blueprint is now saved! Give it to a Builder NPC and they'll construct it for you.

**Quick Radius Method:**

1. Build a structure and stand in the middle
2. Open console with `F5`
3. Type `/scanblueprint MyHouse 15` (15 is the scan radius in meters)
4. Press Enter

Done! The blueprint is saved.

**Sharing Blueprints:**

All blueprints save to `BepInEx/config/blueprints/` as `.yaml` files. You can:
- Copy these files to friends' computers
- Share them on Discord or forums
- Join a server and automatically download their blueprints

### 🤝 Faction System

NPCs belong to different colored teams (Red, Blue, Green, Yellow, Purple). Your actions affect how they feel about you:

**Ways to Gain Reputation:**
- Give food to farmers (+15)
- Complete quests (+35)
- Train skills with priests (+5)

**Ways to Lose Reputation:**
- Hit an NPC (-10 per hit)
- Kill an NPC (-50, makes entire faction hostile!)

**Press F7** to see your reputation with all factions.

**Reputation Levels:**
- 😡 **Hostile** (below -30): NPCs attack you on sight
- 😠 **Unfriendly** (-30 to -10): NPCs refuse to talk
- 😐 **Neutral** (-10 to 10): Normal interactions
- 🙂 **Friendly** (10 to 30): Better prices and rewards
- 🤩 **Honored** (above 30): Best relationship, special perks

### 🎯 Quests

Talk to NPCs to receive quests:

- **Hunt Quests** - Kill specific creatures
- **Delivery Quests** - Take messages to other NPCs
- **Gather Quests** - Collect items
- **Search Quests** - Find hidden locations

Complete quests to earn gold, reputation, and rewards!

### 🏕️ Odin's Camp

Odin himself appears in your world with a mystical camp. Use console commands to move it:

- `/odinhere` - Bring Odin's camp to your location
- `/whereodin` - Show where the camp is
- `/setodin` - Permanently save Odin's position

At Odin's camp you can:
- Trade items for Odin Credits
- Buy special meads and buffs
- Summon temporary pets (wolves with backpacks, trolls that fight for you)
- Raise your skills with the Shaman

### 🍺 Special Meads

Purchase from Odin's store:

- **Exp Meads** - Level up skills faster (Small/Medium/Large)
- **Weight Meads** - Carry more items (Small/Medium/Large)
- **Invisible Meads** - Hide from enemies (Small/Medium/Large)
- **Weapon Meads** - Boost specific weapon damage (Pickaxe/Bow/Sword/Axe)
- **Speed Mead** - Move faster

## Installation (Easy Steps)

**What You Need:**
1. Valheim (version 0.221.12 or newer)
2. BepInEx mod framework

**Installation:**

1. **Install BepInEx** (if you don't have it):
   - Download [BepInExPack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
   - Extract the zip file
   - Copy all folders to your Valheim game folder (where `valheim.exe` is)
   - Run the game once, then close it (this creates the mod folders)

2. **Install OdinPlus**:
   - Download `OdinPlus.dll`
   - Put it in `BepInEx/plugins/` folder (inside your Valheim game folder)
   - Launch Valheim!

**Finding Your Valheim Folder:**
- Steam: Right-click Valheim → Manage → Browse Local Files
- Usually: `C:\Program Files (x86)\Steam\steamapps\common\Valheim\`

## How to Use

### Finding Villages

1. Press `F5` to open console
2. Type `/findfarm` and press Enter
3. Look at your map - a farm location is now revealed!
4. Travel there to meet the NPCs

### Getting Builders to Work

1. Find a Builder NPC at a village (looks like a worker with tools)
2. Press `E` near them
3. Select "What do you need?" to see required materials
4. Give them wood and stone by pressing `E` while holding items
5. Watch them build your blueprint piece by piece!

### Training Skills with the Priest

1. Find the Priest NPC at a village (wears robes)
2. Press `E` to talk, select "Train Skills"
3. Make sure you have 100 gold coins
4. Hold `Left Shift` and press a number key:
   - `1` = Swords
   - `2` = Axes
   - `3` = Bows
   - `4` = Blocking
   - `5` = Running
   - `6` = Jumping
   - `7` = Sneak
   - `8` = Swimming

Your skill instantly goes up by 5 levels!

### Creating Your Own Blueprints

**Visual Method (Recommended):**

1. Build a house or structure anywhere
2. Open console (`F5`)
3. Type: `/selectblueprint TowerHouse`
4. Click one corner of your building
5. Click the opposite corner
6. **Scroll your mouse wheel** to make the blue box taller/shorter until it covers everything
7. Click once more to save

Done! Your blueprint is saved in `BepInEx/config/blueprints/TowerHouse.yaml`

**Sharing Your Blueprint:**
- Find the `.yaml` file in `BepInEx/config/blueprints/`
- Send it to friends via Discord, email, etc.
- They put it in their `blueprints/` folder
- Now they can build your design too!

### Console Commands (Press F5 to Open Console)

| Command | What It Does |
|---------|--------------|
| `/odinhere` | Bring Odin's camp to where you're standing |
| `/whereodin` | Show Odin's camp location on map |
| `/whereami` | Show your coordinates |
| `/findfarm` | Reveal nearest village on map |
| `/selectblueprint MyHouse` | Start blueprint selection mode |
| `/scanblueprint MyHouse 15` | Quick scan (15 meter radius) |
| `/listblueprints` | Show all saved blueprints |
| `/previewscan 15` | See what area will be scanned (yellow spheres) |

### Checking Faction Reputation

Press **F7** at any time to see your standing with all factions. A panel appears showing:
- Faction names and colors
- Your reputation tier (Hostile, Neutral, Friendly, etc.)
- Current reputation number

## Tips & Tricks

💡 **For Beginners:**
- Visit villages early to start earning reputation
- Don't attack NPCs unless you want to make enemies!
- Give farmers food regularly for steady reputation gains
- Use `/previewscan 15` before scanning to check your area

💡 **For Builders:**
- Build simple structures first to test the system
- Use the scroll wheel to adjust height when scanning multi-story buildings
- Builders construct buildings at their current location, so position them where you want the structure
- Give builders extra materials - they'll save them for the next blueprint

💡 **For Explorers:**
- Each village has 4-7 NPCs walking around
- Guards patrol a large area (60 meters from spawn)
- Messengers give quests that lead to other parts of the world
- Faction relationships affect ALL NPCs in that faction

💡 **For Multiplayer:**
- Server hosts can create blueprints that auto-download to all players
- Reputation is tracked per-player
- Builders work the same in multiplayer as single-player
- Share your blueprint `.yaml` files with server friends

## Troubleshooting

**"I can't find any NPCs!"**
- Use `/findfarm` console command to reveal village locations
- Villages spawn at "WoodFarm1" locations in the world
- You may need to travel 500+ meters from spawn to find one

**"Builder NPC isn't moving!"**
- NPCs walk around slowly (10 meter radius)
- They patrol every 20 seconds, so wait a bit
- Guards move faster and farther than other NPCs

**"Blueprint didn't capture my whole building!"**
- Use `/selectblueprint` method instead of `/scanblueprint`
- Scroll your mouse wheel to extend the blue box higher/lower
- Make sure both corners are at ground level

**"Priest won't train my skills!"**
- You need 100 gold coins per skill
- Hold `Left Shift` while pressing number keys (not just press the numbers)
- Make sure you're close enough to the priest

**"NPCs are hostile to me!"**
- Check your reputation with `F7`
- If below -30, you're Hostile with that faction
- Stop attacking NPCs and give items to their enemies instead
- Reputation can always be rebuilt, but it takes time

**"Blueprint doesn't work in multiplayer!"**
- Make sure server has OdinPlus installed
- Blueprints auto-sync when you join
- If not syncing, manually copy `.yaml` files to server's `blueprints/` folder

## Compatibility

**Works With:**
- Valheim Plus
- PlanBuild (can scan PlanBuild structures!)
- Infinity Hammer (can scan IH-placed pieces!)
- BuildShare
- Most other mods

**Requires:**
- Valheim 0.221.12 or newer
- BepInEx 5.4.23.3 or newer

## Configuration Files (Advanced)

If you want to customize the mod, edit these files in `BepInEx/config/`:

- `faction_config.yaml` - Change faction relationships (allies/enemies)
- `faction_quests.yaml` - Add custom quests
- `blueprints/` folder - All your saved blueprints
- `buzz.valheim.OdinPlus.cfg` - Hotkeys and settings

**All files auto-generate on first run** - you don't need to create them manually!

## Support & Community

- **Found a bug?** Report it on Nexus Mods or GitHub
- **Need help?** Ask in the mod comments
- **Made a cool blueprint?** Share your `.yaml` file with the community!

## Credits

**OdinPlus Mod Team**
- Original development and design
- Nexus Mods ID: 798
- BepInEx GUID: `buzz.valheim.OdinPlus`

**Special Thanks:**
- Valheim community for feedback and testing
- BepInEx team for the modding framework
- Blueprint contributors for shared designs

---

**Version:** 0.2.7  
**Last Updated:** August 2026  
**Valheim Version:** 0.221.12+

Enjoy your living Valheim world! 🏰⚔️🌲
