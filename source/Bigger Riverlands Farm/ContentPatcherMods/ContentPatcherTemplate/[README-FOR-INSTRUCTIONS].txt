This is a Content Patcher Template. It does nothing on its own. remove the //comments after you're done filling out the blanks, and you can also delete this README file too

YOU WILL NEED TO DOWNLOAD SMAPI(https://smapi.io/) & CONTENT PATCHER(https://www.nexusmods.com/stardewvalley/mods/1915) IN ORDER TO RUN THESE MODS

PROCEDURE:
1. Put your edited xnb files into assets folder
2. Start editing your content.json file. 
3. Start editing your manifest.json file.
4. Upload to Nexus Mods (or other sites like Github)

This method of PATCHING content is much better. A chart below all this info in this README.txt which was directly taken from: https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher#intro

MORE HELP HERE(looks overwhelming but if you know how to xnb mod, you can figure this out): "https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher"

-------------------------------------------------------https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher------------------------------------------------------------------------------
  	                           XNB mod 	                      Content Patcher
Easy to create 	✘ need to unpack/repack files 	✓ edit JSON files

Easy to install 	✘ different for every mod 	✓ drop into Mods

Easy to uninstall 	✘ manually restore files 	✓ remove from Mods

Update checks 	✘ no 	                                   ✓ yes (via SMAPI)

Compatibility checks ✘ no 	                                   ✓ yes (via SMAPI DB)

Mod compatibility 	✘ very poor                                 ✓ high
(each file can only be changed by one mod) 	(mods only conflict if they edit the same part of a file)
                                                                                                 
Game compatibility 	✘ break in most updates 	✓ only affected if the part they edited changes

Easy to troubleshoot ✘ no record of changes 	✓ SMAPI log + Content Patcher validation (https://log.smapi.io/ to post errors)

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
After you are done creating the mod, post it on Nexus with install instructions on the description (below you can copy)

INSTALLATION
1. Install Latest Version of SMAPI
2. Install Latest Version of Content Patcher
3. Download this mod and extract/unzip into "Stardew Valley/Mods" folder 