# HollowKnight.GODump

Dump game objects in **Hollow Knight** into individual sprites and audio clips. Makes it easier to customize textures in the game.

## Installation

# From Scarab mod installer
1. Download the [Scarab mod installer]( https://github.com/fifty-six/Scarab/releases ) for Hollow Knight 1.5.
2. Run the Scarab executable. The modding API will automatically be installed.
3. Check the "Installed" checkbox next to  "Game Object Dump" from the mod list.

# Manually
1. Install the latest release of the [Modding API]( https://github.com/hk-modding/api/releases ) for Hollow Knight from GitHub. Installation instructions are in the ReadMe.
2. Download the latest release of [GODump]( https://github.com/jngo102/HollowKnight.GODump/releases ) from GitHub.
3. Extract the zip to `Steam/steamapps/common/Hollow Knight/hollow_knight_Data/Managed/Mods/GODump`.


## Usage
1. Start the game and a GODump.GlobalSettings.json file in your Hollow Knight saves directory will be generated (delete GODump.GlobalSettings.json before you start if you're updating from an older version).
2. Enter a gameplay scene.
3. Press F2 and atlases will be generated in `Steam/steamapps/common/Hollow Knight/hollow_knight_Data/Managed/Mods/GODump/Atlases`.The naming convention of an atlas is Animation1@Animation2@...@AnimationN#AtlasName.png
4. Press F3 and GODump.GlobalSettings.json will be updated with a new string of animations in the current scene.
5. Delete the animations you don't want from the resulting pipe-separated ("|") string and save it (Get the animation name from step 3).
6. Press F4 and all sprites in the animations you chose will be dumped into the `Steam/steamapps/common/Hollow Knight/hollow_knight_Data/Managed/Mods/GODump/Sprites` folder in .png format.

## Notice

* The SpriteBorder is the editable border of the sprite.
* **DumpSpriteInfo** Whether to dump a JSON file containing sprite info into the `0.Atlases` folder. Pack needed.
* **DumpAnimInfo** Whether to dump a JSON file containing animation info into each sprites folder.
* **FixSpriteSize** Whether to fix the area outside the red rectangular of a sprite. Pack with v1.2 selected if Not Fix. Pack with v1.3 selected if Fix.
* **SpriteBorder** Whether to add a red rectangle surrounding each sprite.
* **AnimationsToDump** A pipe-separated ("|") string that lists which animations to dump.
* **AudioClipsToDump** A pipe-separated ("|") string that lists which audio clips to dump.

## Update

* **v1.2** Change naming of sprites to "Animation Num - Frame Num - Collection Num" from "Collection Num" only.Slice sprites one pixel lower than before.Use ** SpritePacker** to pack sprites back into atlas.
* **v1.3** Add setting **SpriteSizeFix**.Cutted empty space of a sprite in an atlas by tk2d tool is now added back.**No More Worry About Where The Fuck is The Anchor!**
* **v1.4** Press F2 to check for atlases you need;Press F3 to print all the animations in the scene
* **v1.5** Simplify json file on API 57. Add **RedRectangular** setting.
* **v1.6** Updated for Hollow Knight 1.5. Added audio clip dumping.

## Credits
* [magegihk](https://github.com/magegihk) - Creator of the original GODump mod.
* [KayDeeTee](https://github.com/KayDeeTee) - SpriteDump Mod save me a lot of time to figure out how to dump pngs.
* [Serena](https://github.com/seresharp), [56](https://github.com/fifty-six) - Modding API.
* [Team Cherry](https://teamcherry.com.au/) - Without which, we would not have Hollow Knight.

## License
[GPL-3.0](https://choosealicense.com/licenses/gpl-3.0/)