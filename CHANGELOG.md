## Changelog

**?.?.? Changes:**

* Added 13 new effects:
  * Bouncy Items: Item drops will bounce on the ground before settling, lasts 60 seconds
  * Increase Skill Charges: Adds 1 charge to every skill, lasts 1 stage
  * Decrease Skill Charges: Removes 1 charge from every skill (cannot reduce below 1), lasts 1 stage
  * Mystery Items: All item pickup models are hidden, lasts 60 seconds
  * Focused Teleporter Charging: Holdout zone radius decreases with charge percentage, lasts 1 stage
  * Sluggish Camera: Delays camera position by a small amount, lasts 45 seconds
  * Increase Recoil: Increases recoil by 10x, lasts 90 seconds
  * No Recoil: Disables all recoil, lasts 90 seconds
  * High FOV: Increases camera Field of View, lasts 90 seconds
  * Low FOV: Decreases camera Field of View, lasts 90 seconds
  * Flipped Camera: Flips the camera upside down, lasts 30 seconds
  * Void Implosion on Death: Spawns a void implosion on any character death, lasts 1 stage
  * Inverted Recoil: All recoil is inverted, lasts 90 seconds

* Removed 1 effect:
  * Sequence All Players
    * Almost always killed you if it activates late-run, did basically nothing if it activates early

* Effect selection:
  * Added effect seeding option
    * Picks effects based on run seed instead of randomly picking each time. Use if you are setting run seeds manually, otherwise it is functionally identical to normal mode (Not supported in chat voting mode)
  * Added per-stage effect list option
    * A portion of all effects are picked out each stage to be activatable instead of all effects (Not supported in chat voting mode)

* Effect activation:
  * Fixed effects not being able to activate in Void Fields
  * Effects that disallow duplicates will now add to the active effect's timer instead of not being activatable.
  * Fixed new effects not having priority over active ones. All active incompatible effects will be ended instead.

* UI:
  * Added option to display the next effect that will happen.
    * Only supported in seeded mode and with chat voting disabled.

* Added ProperSave support
  * Active effects and internal state are saved at the start of each stage

* Mitosis:
  * Fixed spawned copies overlapping the original's collider, resulting in flying characters getting flung away

* Give Everyone a Random Buff & Debuff:
  * Now gives several stacks if the random buff/debuff is stackable.

* Give Everyone a Random Buff:
  * WolfoQoL compatibility
  * Default duration: End of stage -> 90s

* Give Everyone a Random Debuff:
  * Default duration: End of stage -> 60s

* Add Random Item to Monster Inventory:
  * Added config option for applying the enemy item blacklist, enabled by default

* One Hit KO:
  * Fixed barrier not decaying during effect
  * All characters will now use the fragile death animation while the effect is active

* The Floor is Lava:
  * Changed burn dps: 15% of character damage stat -> 25% of max health per second (configurable)
  * Fixed burn damage being affected by items
  * Fixed burn sometimes not being removed on effect end

* Change Difficulty:
  * Effect can now have multiple instances active at once (can change serveral times per stage)
  * Removed modded difficulty support
    * Too much to fix compatibility-wise, wasn't worth the effort.

* Gambling Addiction:
  * Explicit drops are now carried over.
    * Ex. If a multishop containing a syringe turns into a shrine, the first shrine drop will be a syringe, second item will a random item from the multishop drop table
    * Printers turned into shrines now only drop the original printer item instead of a random item of the same tier

* Randomize Loadout:
  * Added config options to disable effect changing skins or skills
  * Should now no longer give non-unlocked skills and skins to client players in multiplayer

* Randomize Loadout & Enable Random Artifact (Metamorphosis):
  * Desperado stacks are no longer reset

* Adaptive Recycling:
  * Fixed effect being able to occasionally pick invalid items
  * Increased minimum pickup duration: 0.75s -> 1s
  * Fixed effect being able to pick items and equipment not available in the run item pool

* Blood Money:
  * Added config options to exclude specific cost types from the effect
    * Only money and lunar coin costs are enabled by default

* Scrap Random Item:
  * Fixed item notifications not showing for non-host players in multiplayer

* Force Activate Random Skill:
  * Fixed Beetles freezing in place if their secondary is forced

* Disable Random Skill:
  * Added minor visual indicator when a skill is unlocked again

* All Items Are A Random Item:
  * Fixed trishops having items rerolled after being purchased
  * Fixed being able to use Artifact of Command to get around the forced item

* Spawn Random Interactable:
  * Fixed interactables being able to spawn in occupied nodes (inside already spawned objects).

* Spawn Random Portal:
  * Fixed portals being able to spawn in occupied nodes.

* Spawn Jump Pad:
  * Fixed jump pads being able to be spawned in occupied nodes
  * Fixed jump pads not aligning properly to the ground they spawn on

* Revive Dead Characters:
  * Fixed effect reviving players in multiplayer as AI controlled allies

* You and a super intelligent Lemurian...:
  * The Lemurian's projectiles can no longer be deleted (Captain defense matrix, blast shower, etc.)
  * Added separate name for Leonard's elder variant
  * Added custom death messages
  * Gave Leonard his allowance

* Recruit Random Enemy:
  * Added chat message telling you which enemy was recruited

* Orbital Bombardment:
  * Added regular airstrikes to effect

* Loose Pockets:
  * Default duration: 20s -> 10s

* Superhot:
  * Default duration: 90s -> 45s

* Aspect Roulette:
  * Default duration: 90s -> 60s

* Freeze!:
  * Default freeze duration: 4s -> 2.5s

* Misc
  * Fixed strange scrap counting towards game completion in logbook
  * Fixed some effects sometimes placing character VFX at world origin
  * Updated R2API.Core dependency version: 5.0.10 -> 5.0.11
  * Updated Risk of Options dependency version: 2.6.1 -> 2.7.1

**1.12.2 Changes:**

<details>

* Reworked 1 effect:
  * Drop All Items -> Loose Pockets: Drops a random item from everyone's inventory every 0.9 seconds

* Steal All Player Items:
  * Added a limit to how many items each enemy can take from each player, default 2 stacks, configurable.
  * Renamed effect to "Steal Player Items"

* Voidtouch Everyone:
  * Added config option to make effect not voidtouch drones (enabled by default)
  * Added config option to make effect not voidtouch any player allies (disabled by default)
  * Fixed enemy AI sometimes targetting incorrect teams after being voidtouched

* Bouncy Projectiles:
  * No longer bounces off of enemies that would be hurt by the projectile

* Decrease Teleporter Charge Rate:
  * Decreased default charge rate reduction: -50% -> -25%

* All Items Are A Random Item:
  * Fixed Scavenger bags ignoring override item
  * Decreased default selection weight: 1 -> 0.8

* Chat Voting:
  * "Random Effect" option is now guaranteed to not be any of the other options in the vote

</details>

**1.12.1 Changes:**

<details>

* Risk of Thunder:
  * Increased lightning strike radius (3->6)
  * Now properly targets characters instead of just random map locations
  * Damage is now scaled (50 base, +25 per monster level)

* Increase & Decrease Projectile Speed:
  * No longer affects stationary attacks

* Config:
  * Added mod icon to Risk of Options menu

* UI:
  * Added config option to disable the "Active Effects" display

</details>

**1.12.0 Changes:**

<details>

* Added 5 new effects:
  * Inventory Swap: Swaps the inventories of each player with another player. Multiplayer only.
  * No Equipment Cooldowns: Removes all equipment cooldowns, lasts 60 seconds.
  * Disable Equipment: Disables all equipment activation, lasts 60 seconds.
  * All Items Are A Random Item: All items on the stage get turned into a random item. Essentially Artifact of Kin for items. Lasts 1 stage.
  * All Chests are Free: All chests and interactables are free, lasts 30 seconds.

* Randomize Loadout & Enable Random Artifact (Metamorphosis):
  * Fixed character respawn restoring health and shields to full

* Spawn Void Seed:
  * Void Seed can now spawn at any spot on the stage, not just the ones normally available as Void Seed spawn locations

* General:
  * Added config option to disable effect dispatching while run timer is paused

* Twitch Voting:
  * Fixed vote options sometimes not being visible while dispatching is temporarily disabled
  * Improved error messages if the Twitch Client fails to connect

</details>

**1.11.0 Changes:**

<details>

* Added 1 new effect:
  * Relocate Teleporter: Moves the stage teleporter to a random position on the map

* Change Difficulty:
  * The new difficulty now has a duration instead of for rest of the run
  * Default weight: 0.2 -> 0.6

* Aspect Roulette:
  * Fixed boss health bars not updating to match the new elite aspect

* Scrambled Text:
  * Fixed boss health bar text not updating if effect is activated after the boss spawned

* Corrupt Random Item:
  * Added configurable blacklist to make the effect not corrupt certain items

* Give Random Item & Add Random Item to Monster Inventory:
  * Added configurable blacklist to exclude specific items or equipments from the effect
  * Amount of items given is now configurable

* Give Tonic Affliction:
  * Amount of affliction given to each player is now configurable

* Steal All Player Items:
  * Improved stealing interval to be a bit more spread out
  * Added config for blacklisted items (won't be stolen)
  * Added config to make enemies ignore AI blacklist while using your items

* Scrap Random Item:
  * Added config to control how many items or stacks are scrapped per player
  * Added item blacklist config
  * Fixed item scrap pickup message appearing before effect activation message

* Uncorrupt Random Item:
  * Added config to change how many item stacks are uncorrupted per player
  * Added item blacklist config

* Unscrap Random Item:
  * Added config to change how many scrap stacks are unscrapped per player
  * Added item blacklist config

* Benthic Transform Random Item:
  * Added config to change how many items are transformed per effect activation

* Disable Random Skill & Force Activate Random Skill:
  * Added config to exclude specific skill slots from the effect

* Launch Everyone in Random Directions:
  * Added config to control the strength of the force applied to characters
  * Effect will now always launch players upwards if fall damage is lethal to prevent the effect from instantly killing you

* Item Magnet & Item Repulsor:
  * Added config to control the strength of the item attraction/repulsion

* Adaptive Recycling:
  * Added config for amount of time between recycles

* You and a super intelligent Lemurian...:
  * Now has a 5% chance to spawn as an Elder
  * Now has a 33% attack speed reduction
  * Fixed Leonard not attacking any enemies if he was recruited to the player team

* Recruit Random Enemy:
  * Recruited enemy will now come with you to the next stage
 
* Potrolling:
  * Added config for amount of pots spawned

* Mod Compatibility:
  * Fixed mod effectively removing all modded damage types

* Misc:
  * Updated R2API.Core dependency: 5.0.3 -> 5.0.10
  * Updated R2API.RecalculateStats dependency: 1.0.0 -> 1.2.0
  * Updated R2API.Prefab dependency: 1.0.1 -> 1.0.3

</details>

**1.10.1 Changes:**

<details>

* Added 2 new effects:
  * Increase Skill Cooldowns: Increases cooldown for all skills, lasts 1 stage
  * Decrease Skill Cooldowns: Decreases cooldown for all skills, lasts 1 stage

* Sulfur Pools Experience:
  * Fixed spawned pods not aligning with the ground properly

* Randomize Loadout:
  * Skip spawn animation when respawning

* Randomize Loadout & Enable Random Artifact (Metamorphosis):
  * No longer resets Eclipse 8 curse stacks

* Misc:
  * Seconds duration of effects is now displayed in the effect voting options and the chat activation message

</details>

**1.10.0 Changes:**

<details>

* Added 9 new effects:
  * Recruit Random Enemy: Converts a random enemy on the stage to the player team
  * Adaptive Recycling: Repeatedly recycles all items on the stage, lasts 90 seconds
  * Decrease Teleporter Charge Rate: Decreases charge rate for all holdout zones, lasts 1 stage
  * Increase Teleporter Charge Rate: Increases charge rate for all holdout zones, lasts 1 stage
  * Decrease Teleporter Radius: Decreases the radius on all holdout zones, lasts 1 stage
  * Increase Teleporter Radius: Increases the radius on all holdout zones, lasts 1 stage
  * Scrambled Text: Randomizes the order of letters in most text displayed in the game, lasts 120 seconds
  * Sulfur Pools Experience: Fills the map with Sulfur Pods
  * Disable Knockback: Disables all knockback, lasts 1 stage

* Add Random Item to Monster Inventory:
  * Effect can now be set to any duration type in the config, default is still for the rest of the run

* One Hit KO:
  * Now sets everything to 1 hp for a duration instead of just once, default 30 seconds

* Mitosis:
  * Fixed clones of bosses not counting as bosses

* Disable Fall Damage:
  * Fixed Safer Spaces being triggered when fall damage would normally happen

* Unscrap Random Item:
  * Fixed showing item notification twice

* You and a super intelligent Lemurian...:
  * Leonard

* Misc:
  * Added config options to change the colors of most UI elements of the mod

</details>

**1.9.1 Changes:**

<details>

* Added 1 new effect:
  * Delayed Attacks: All attacks have a 0.5 second delay before happening, lasts 90 seconds

* Superhot:
  * Fixed time scale being really slow when players are unable to move (frozen, in cutscene, etc)

* Revive Dead Characters:
  * Revived enemies now give gold and exp when killed

* Spawn Random Enemy, Ally, & Boss:
  * Added chance for spawned characters to be elites

* Activate Random Equipment:
  * Now activates equipments on all characters, not just players

* Spawn Random Interactable:
  * Added Lunar Cauldrons to spawn pool
  * Added Newt Altar to spawn pool

* Freeze!:
  * Freeze duration can now be configured

* Force Activate Random Skill:
  * Changed default duration: Until next stage -> 90s

* Disable Random Skill:
  * Changed default duration: Until next stage -> 90s

* Spawn Random Ally:
  * Fixed Grandparents spawning in the air
  * Fixed effect not using ally skins of characters that have them

* Max All Cooldowns:
  * Fixed Railgunner not being able to fire scoped shots after effect activation

* Aspect Roulette:
  * Can no longer select elites with a tier outside of those available on the current stage by default, old behaviour can be re-enabled in the config

* Misc:
  * Added the ability to set a keyboard shortcut to activate a specific effect at any time in a run
  * Updated default effect weight reduction percentage: 5% -> 0%

</details>

**1.9.0 Changes:**

<details>

* Added 5 new effects:
  * No sprinting: Disables sprinting for all characters, lasts 30 seconds
  * Everyone is Invisible: Every character on the stage becomes invisible, lasts 30 seconds
  * Revive Dead Characters: Revives all recently killed characters
  * The Floor is Lava: Every character touching the ground is set on fire, lasts 30 seconds
  * Lock All Chests: Locks all chests as if the teleporter has started, lasts 45 seconds

* Duplicate Random Item Stack:
  * Added config option to blacklist specific items from being duplicated
  * Added config option to disallow duplication if the item count is greater than some value (default 1000)

* Gravity effects:
  * Jump pads will now always bring players to the same location, regardless of the current gravity
  * Gravity effects can now be activated on Commencement, they were previously blacklisted from the stage to prevent run softlocks with the jump pads up to Mithrix not bringing you all the way up

* Random Gravity Direction:
  * Fixed characters sliding slowly in the gravity direction when grounded

* Combo:
  * Fixed effect selection ignoring incompatibility rules

* Superhot:
  * Default duration: Until next stage -> 90s

* Renamed effect: Touch Void -> Voidtouch Everyone

* Activate Teleporter, +2 Mountain Shrine, & Eradicate Random Item:
  * No longer credits the host player in the chat message, it now properly communicates it was the mod's doing instead.

* All Items are Void Potentials:
  * Fixed duplicate items drops if the effect is activated while Artifact of Command is active

</details>

**1.8.0 Changes:**

<details>

* Added 6 new effects:
  * Aspect Roulette: Randomly switches the elite aspect of all characters (only affects players if they already have an aspect equipment)
  * Unscrap Random Item: Converts a random stack of scrap into a random item of the same tier
  * Disable Procs: Disables all proc effects. Lasts 45 seconds
  * Item Magnet: All pickups move towards players. Lasts 90 seconds
  * Item Repulsor: All pickups move away from players. Lasts 90 seconds
  * Kill All Player Allies: Kills all player allies

* Removed 1 effect:
  * Respawn As Random Character: Either just respawned you as a survivor (which is just Metamorphosis activating), or as an enemy character, which would just guarantee death 9 times out of 10

* Stability:
  * Improved error handling for certain effects.

* Drop All Items:
  * Fixed effect not working

* Give Everyone a Random Buff & Debuff:
  * Added proper mod compatibility with Starstorm 2, LostInTransit, VanillaVoid, MysticsItems, TsunamiItemsRevived, ExtradimensionalItems, and SpireItems

* Increase Proc Coefficients:
  * Fixed proc coefficient multiplier being applied several times per attack

* Increase Director Credits:
  * Renamed effect: Increase Director Credits -> Increase Monster Spawns

* Effect Voting:
  * Added error message if the Manual Reconnect button is pressed when not logged in. Hopefully reduces the number of confused streamers. Hopefully.

* Misc:
  * Added option to disable automatic effect dispatching
  * Fixed automatic effect activation getting delayed if Rewind Run Timer is activated while the run timer is paused
  * Updated Risk of Options dependency (2.5.3 -> 2.6.1)

</details>

**1.7.4 Changes:**

<details>

* Reworked 1 effect:
  * Pause Physics -> Laggy Physics

* Spawn Random Interactable:
  * Removed Cloaked Chest from spawn pool
  * Removed Deep Void Signal from spawn pool

* Roll Credits:
  * Slightly improved performance while active

</details>

**1.7.3 Changes:**

<details>

* Bouncy Projectiles:
  * Effect can now be activated several times per stage (max number of bounces increases with each instance of the effect)
  * Fixed a horrific and immersion-destroying spelling mistake in Max Projectile Bounce Count config description, I truly apologize for letting such a terrible mistake slip by my rigorous testing, and my heart goes out to those who have lost friends or family members because of this. The "programmer" responsible for this frankly unacceptable act has been thoroughly diciplined.

* Eradicate Random Item:
  * Fixed Strange Scrap not being usable as scrap

</details>

**1.7.2 Changes:**

<details>

* Added 1 new effect:
  * Roll Credits: Starts the game credits

* Steal All Player Items:
  * Added a marker to enemies that have stolen your items

* Teleporting Attacks:
  * Fixed AOE attacks not teleporting the attacker if nothing was hit

</details>

**1.7.1 Changes:**

<details>

* UI:
  * Made active effects display take slightly less vertical space

* Blood Money:
  * Earning money now heals players the same amount of health they would have lost if they spent that amount of money instead

* Bouncy Projectiles:
  * Added bounce functionality to more projectile types

* Superhot:
  * Slightly increased minimum time scale & decreased maximum time scale
  * The time scale now more closely tracks with the player's speed

* Steal All Player Items
  * Fixed dead enemies being able to steal items

* Misc:
  * Fixed effects being able to activate during cutscenes

</details>

**1.7.0 Changes:**

<details>

* UI:
  * Active effects are now displayed on the HUD.

* Teleporting Attacks:
  * Changed Duration: Until next effect -> 30s

* Pause Physics:
  * Changed Duration: Until next effect -> 40s

* Timed Effects
  * The Duration Type of timed effects can now be changed in the config.
    * UntilStageEnd: Lasts until you exit the stage.
    * FixedDuration: Lasts for a set number of seconds.
    * Permanent: Lasts until the end of the run.

</details>

**1.6.0 Changes:**

<details>

* Added 4 new effects:
  * Spawn Jump Pad: Spawns a random jump pad at every player
  * Superhot: Time moves when players move
  * Pause Physics: Pauses all physics objects (not including players or enemies). Lasts until next effect.
  * Gupscare: Spawns a Gup above every player

* One Hit KO:
  * Players will now receive a temorary damage immunity for 0.75 seconds if the effect "deals" more than 20% of their max health (basically if you had over 20% health before the effect activates). This helps prevent situations where the effect immediately kills you if it activates while you are in combat.

* Scrap Random Item:
  * Now converts *all* of an item stack into scrap, not just 1 of the items from that stack. Old behavior can be re-enabled in the config.

* Spawn Random Ally & Enemy:
  * Added Col. Droneman to spawn pool

* Invert Knockback:
  * Effect can now be activated several times per stage

* Removed 1 effect:
  * Warbanner: Just caused a bunch of lag, and the warbanner visuals didn't communicate which team it belonged to, making it confusing too.

</details>

**1.5.0 Changes:**

<details>

* Added 9 new effects:
  * Reinforcements: Spawns allied survivors in drop pods around the map.
  * Bouncy Projectiles: All projectiles and bullets bounce on the surface they hit. Lasts 1 stage.
  * Eradicate Random Item: Permanently removes a random item from the game for the rest of the run
  * Reset Player Level: Sets all players' level to 0
  * -5 Minutes: Decreases the run timer by 5 minutes
  * Invert Knockback: Reverses the direction of all knockback applied to characters
  * +100% Fall Damage: Increases fall damage by 100% (configurable). Also makes it lethal. Lasts 1 stage.
  * Disable Fall Damage: Disables all fall damage. Lasts 1 stage.
  * Risk of Thunder: Spawns lightning strikes at random points on the map. Lasts 30 seconds.

* Give Everyone a Random Buff
  * Fixed certain elite effects not being applied properly

* Ahoy!:
  * Fixed drone spawns being affected by Artifact of Swarms

* Mitosis:
  * Fixed allies duplicating being affected by Artifact of Swarms
  * Duplicated allies are now temporary (will not be carried over to the next stage), this is done to prevent lag due to ending up with an unreasonable number of drones. Old behavior can still be re-enabled in the config for the effect.

* Guaranteed Chance Effects:
  * Tougher Times is now excluded from this effect, since blocking all damage is not very interesting

* Increase Director Credits:
  * Credit increase percentage is now configurable

</details>

**1.4.1 Changes:**

<details>

* Added 1 new effect:
  * Steal All Player Items: Steals all items from every player and distributes them among enemies, damage the enemy that took items to gain them back (leaving the stage will also give all the items back)

* All Skills are Agile:
  * Fixed Bandit revolvers (Lights Out & Desperado) not being able to fire while sprinting
  * Fixed Railgunner unscoping while sprinting
  * Fixed Acrid primary not dealing damage while sprinting
  * Fixed MUL-T Nailgun cancelling when sprinting
  * Fixed MUL-T Power Mode cancelling when sprinting
  * Fixed Void Fiend corrupt primary cancelling when sprinting

* World Speed Effects:
  * Increase World Speed:
    * Change default increase amount: +50% -> +25%
  * Decrease World Speed:
    * Change default decrease amount: -50% -> -25%
  * This will not change any existing configs, just the default value if you reset it
  * Fixed extremely slidy player movement if world speed was decreased by a lot
  * Player skills and equipment are now adjusted properly to always have the same realtime cooldown

* Misc:
  * Fixed a bug that would sometimes cause 2 effects to activate at once

</details>

**1.4.0 Changes:**

<details>

* Added 5 new effects:
  * Blood Money: All interactable prices are converted into percent health cost, lasts 1 stage
  * Force Activate Random Skill: Forces a random skill to constantly activate, lasts 1 stage
  * Spawn Random Enemy: Spawns a random enemy for every player
  * Spawn Random Ally: Spawns a random ally for every player
  * Respawn As Random Character: Respawns every player as a random character

* Increase Chest Prices:
  * Fixed percent health costs being able to go above 99%

* Spawn Random Boss:
  * Added Void Devastator to spawn pool

* Enable Random Artifact:
  * Fixed non-player controlled allies not having the effect properly applied when Artifact of Glass is enabled

* Spawn Void Seed:
  * Fixed effect being able to activate if the stage doesn't allow one to spawn

</details>

**1.3.0 Changes:**

<details>

* Added 5 new effects:
  * Guaranteed Chance Effects: All percent-chance effects are guaranteed to happen (effectively infinite luck stat on everything), lasts 1 stage
  * Increase Projectile Speed: Increases the speed of all projectiles, lasts 1 stage (+50% by default, configurable)
  * Decrease Projectile Speed: Decreases the speed of all projectiles, lasts 1 stage (-50% by default, configurable)
  * Increase World Speed: Increases the game speed, but compensates all players to be slower, gives the illusion of everything else being faster, lasts 1 stage (+50% by default, configurable)
  * Decrease World Speed: Decreases the game speed, but compensates all players to be faster, gives the illusion of everything else being slower, lasts 1 stage (-50% by default, configurable)

* Add Random Item to Monster Inventory:
  * Fixed effect not giving items to void or lunar enemies
  * Now gives items to all active enemies when the effect activates, not just future spawned ones

* Give Everyone a Random Buff (& Debuff):
  * Fixed buffs that cannot be stacked being applied several times if effect activates several times per stage.

* Multiplayer:
  * Fixed various potential server-client desync issues

* Twitch Voting:
  * Votes will now alternate being offset by vote option amount to prevent identical vote chat messages being blocked (for example, by default, every other vote will be 1-4 and 5-8)
  * Added "Manual Reconnect" button in Streamer Integration config. Which can be used to reconnect the mod to your Twitch channel in case it gets disconnected and is unable to automatically reconnect.

</details>

**1.2.1 Changes:**

<details>

* Twitch Voting:
  * Fixed certain effects never being able to activate when effect voting was enabled
  * Fixed a few cases where the vote display would not appear when entering a new stage until the next vote starts

* Spawn Random Interactable:
  * Now spawns one interactable per player instead of just at one random player

* Increase Proc Coefficients:
  * Fixed missing duration in display name

</details>

**1.2.0 Changes:**

<details>

* Added 8 new effects:
  * All Items Are Void Potentials: All dropped items become Void Potentials. The original item is always guaranteed to be an option to prevent potential softlocks. Lasts 1 stage.
  * All Skills are Agile: Allows every skill to be used while sprinting. Lasts 1 stage.
  * Give Everyone a Random Buff: Gives every character on the map a random buff for the rest of the current stage.
  * Give Everyone a Random Debuff: Gives every character on the map a random debuff for the rest of the current stage.
  * Moon Detonation: Starts the moon escape sequence. Lasts 45 seconds (configurable)
  * Spawn Random Interactable: Spawns a random interactable at a random player
  * Spawn Random Portal: Spawns a random portal at a random player
  * Increase Proc Coefficients: Multiplies all proc coefficients by 2 (configurable)

* Twitch Voting:
  * Fixed "Vote Winner Selection Mode" setting not applying when changed from in-game
  * Added config for changing vote display scale.
  * Slightly decreased default size of vote display.

* Config:
  * Removed "Effect Repetition Reduction Percentage" and "Effect Repetition Count Mode" configs for effects that can only activate once per stage

* Give Random Item & Add Random Item to Monster Inventory:
  * Removed Consumed items
  * Added Pearl and Irradiant Pearl

* Spawn Scavenger Bag
  * Now has a 1/5 chance of spawning a Lunar Scavenger bag

</details>

**1.1.0 Changes:**

<details>

* Added 1 new effect:
  * Spawn Void Seed: Spawns a void seed somewhere on the map

* Twitch Integration:
  * Voting mode can now be changed at any time during a run

* Ahoy!
  * Fixed an issue where the effect would frequently spawn less drones than it was supposed to

* Drop All Items:
  * Now drops items from all active characters, not just players

* Max All Cooldowns:
  * Now affects all characters, and not just players

* Give Random Item & Add Random Item to Monster Inventory:
  * Added Items to Pool:
    * Artifact Key
    * Defensive Microbots
    * Tonic Affliction
    * All Consumed Items

* You and a super intelligent Lemurian...:
  * Can no longer target non-player controlled characters (no more hiding behind your drones)
  * Fixed an issue where the lemurian would sometimes not have complete vision of the entire map

* Spawn Void Implosion:
  * Added Void Jailer implosion

* Spawn Random Boss:
  * Fixed DLC bosses being able to spawn without SOTV enabled

</details>

**1.0.0 Changes:**

<details>

* Twitch Integration:
  * Added Twitch Integration

* Performance:
  * Fixed a significant lagspike when a random effect is selected for the first time

</details>

**0.9.0 Changes:**

<details>

* Added 4 new effects:
  * Disable Random Skill: Disables a random skill slot (Lasts 1 stage)
  * Ahoy!: Spawns 3 equipment drones with a Consumed Trophy Hunter's Tricorn
  * Increase Knockback: Multiplies all knockback by 3 (configurable), lasts 1 stage
  * Add Random Item to Monster Inventory: Permanently adds a random item to all enemies

* Touch Void:
  * Void infested allies no longer stay across stage transitions

* Multiplayer:
  * Fixed various server-client desync issues

</details>

**0.8.1 Changes:**

<details>

* Added 5 new effects:
  * Spawn Random Beacon: Spawns a random captain beacon on every player
  * Orbital Bombardment: Spawns Diablo Strikes all over the map
  * Benthic Transform Random Item: Upgrades the tier of 1 random item
  * Kill All (Non-Boss) Enemies: Kills all non-boss enemies on the map
  * Random Gravity Direction: Changes the direction of gravity

* Randomize Loadout: Fixed chat issues (for real this time)

* Give Tonic Affliction: Now prints a chat pickup message

* Spawn Random Boss: Now supports Artifact of Swarms properly

* Fixed player teleporting effects not working on clients

* Fixed "Teleporting Attacks" not being able to activate more than once per stage

* Misc: Minor performance improvements

</details>

**0.8.0 Changes:**

<details>

* Potrolling: Pots now have invincibility for 1 second after spawning, so they can no longer explode immediately

* Randomize Loadout: Fixed an issue with the in-game chat after the effect activates.

* Increase/Decrease Chest Prices:
  * All cost types are now changed by these effects.
  * Gold and Health costs can now reach 0 with enough decrease.

* Added 10 new effects:
  * Combo: Activates 2 other random effects
  * Gambling Addiction: Replaces every source of loot on the map with a chance shrine
  * Give Tonic Affliction
  * Spawn Random Boss
  * Max All Cooldowns: Sets all skill and equipment cooldowns to their maximum value (as if you just used them)
  * Teleporting Attacks: Teleports the attacker to where their attacks impact
  * Uncorrupt Random Item: Converts all of a random item into its non-void variant
  * Poverty: Sets all players' money to 0
  * +5 Minutes: Adds 5 minutes to the run timer
  * Trigger Random Family Event: Activates a random family event for the rest of the current stage

</details>

**0.7.0 Changes:**

<details>

* Effects will now activate in stages with the run timer paused
* Fixed effects being able to activate while the game is paused if the time between effects config value is changed

* Added Effect: Teleport to Random Location
* Added Effect: Activate Random Equipment
* Added Effect: Change Difficulty

</details>

**0.6.0 Changes:**

<details>

* Added Effect: Increase Chest Prices
* Added Effect: Decrease Chest Prices
* Added Effect: Spawn Void Implosion
* Added Effect: Launch Everyone in Random Directions
* Added Effect: Touch Void
* Added Effect: Duplicate Random Item Stack

* Drop All Items: Decreased chance of the effect happening many times per run

* Added config entries for effect weight reduction per activation (decrease likelyhood of effect activating many times)

</details>

**0.5.0 Changes:**

<details>

* Added Effect: Potrolling
* Added Effect: Wet Floor

* Fixed changing the time between effects config mid-run not applying properly

</details>

**0.4.0 Changes:**

<details>

* Added Effect: Warbanner
* Added Effect: Spawn Doppelganger

* Give Random Item: Fuel Cell and Elegy of Exctinction have been added the the equipment pool

* Effects giving equipment will now prioritize the active equipment slot first, then continue looking for empty ones. If no empty slots are found, the current equipment slot is overriden (old equipment is dropped at the player's feet)

* Decreased likelyhood of Gravity-based effects happening several times per stage

* Fixed +50% Director Credits not properly applying more than once per stage

</details>

**0.3.0 Changes:**

<details>

* Added effect: Corrupt Random Item

* The mod now requires every player to have the mod installed in multiplayer
  * This will make it much easier to add new (and more complex) effects in the future.

* Fixed Gravity effects not applying properly to non-host players.

* Fixed Enable Random Artifact not immediately applying health and damage stat changes when Artifact of Glass was selected

* Fixed Randomize Loadout only giving default skills and skins

</details>

**0.2.0 Changes:**

<details>

* Added effect: Give Random Elite Aspect
* Fixed Randomize Loadout forcing players out of the intro pod
* Fixed Randomize Loadout giving players skills or skins they didn't have unlocked
* Give Random Item: If giving equipment, it will now search all equipment slots for an empty one instead of just the active one, and if none are found, the equipment will be dropped at the players feet instead.
* Payday: Added config options to control how much money is given and if it should scale the amount given with interactible prices

</details>

**0.1.8 Changes:**

<details>

* Removed (now unnecessary) R2API.Networking dependency
* Fixed language tokens not loading due to invalid folder structure in last upload (oops)

</details>

**0.1.7 Changes:**

<details>

* Added effect: +50% Gravity
* Added effect: -50% Gravity
* Fixed +50% Director Credits carrying over to future stages (would still apply to directors after stage load)

</details>

**0.1.6 Changes:**

<details>

* Fixed language tokens not loading (for real this time)

</details>

**0.1.5 Changes:**

<details>

* Added effect: Scrap Random Item
* Randomize Loadout will no longer revive dead players

</details>

**0.1.4 Changes:**

<details>

* Added effect: Mitosis
* Use R2API split assemblies

</details>

**0.1.3 Changes:**

<details>

* Added effect: You and a super intelligent Lemurian...

</details>

**0.1.2 Changes:**

<details>

* Fix Randomize Loadout spawning players as a new survivor if Artifact of Metamorphosis was enabled.
* (Hopefully) fix language tokens sometimes not loading properly

</details>

**0.1.1 Changes:**

<details>

* README update

</details>

**0.1.0 Changes:**

<details>

* First release

</details>