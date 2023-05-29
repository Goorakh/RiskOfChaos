## Changelog

**1.7.3 Changes:**

* Bouncy Projectiles:
  * Effect can now be activated several times per stage (max number of bounces increases with each instance of the effect)
  * Fixed a horrific and immersion-destroying spelling mistake in Max Projectile Bounce Count config description, I truly apologize for letting such a terrible mistake slip by my rigorous testing, and my heart goes out to those who have lost friends or family members because of this. The "programmer" responsible for this frankly unacceptable act has been thoroughly diciplined.

* Eradicate Random Item:
  * Fixed Strange Scrap not being usable as scrap

**1.7.2 Changes:**

* Added 1 new effect:
  * Roll Credits: Starts the game credits

* Steal All Player Items:
  * Added a marker to enemies that have stolen your items

* Teleporting Attacks:
  * Fixed AOE attacks not teleporting the attacker if nothing was hit

**1.7.1 Changes:**

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

**1.7.0 Changes:**

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

**1.6.0 Changes:**

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

**1.5.0 Changes:**

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

**1.4.1 Changes:**

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

**1.4.0 Changes:**

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

**1.3.0 Changes:**

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

**1.2.1 Changes:**

* Twitch Voting:
  * Fixed certain effects never being able to activate when effect voting was enabled
  * Fixed a few cases where the vote display would not appear when entering a new stage until the next vote starts

* Spawn Random Interactable:
  * Now spawns one interactable per player instead of just at one random player

* Increase Proc Coefficients:
  * Fixed missing duration in display name

**1.2.0 Changes:**

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

**1.1.0 Changes:**

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

**1.0.0 Changes:**

* Twitch Integration:
  * Added Twitch Integration

* Performance:
  * Fixed a significant lagspike when a random effect is selected for the first time

**0.9.0 Changes:**

* Added 4 new effects:
  * Disable Random Skill: Disables a random skill slot (Lasts 1 stage)
  * Ahoy!: Spawns 3 equipment drones with a Consumed Trophy Hunter's Tricorn
  * Increase Knockback: Multiplies all knockback by 3 (configurable), lasts 1 stage
  * Add Random Item to Monster Inventory: Permanently adds a random item to all enemies

* Touch Void:
  * Void infested allies no longer stay across stage transitions

* Multiplayer:
  * Fixed various server-client desync issues

**0.8.1 Changes:**

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

**0.8.0 Changes:**

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

**0.7.0 Changes:**

* Effects will now activate in stages with the run timer paused
* Fixed effects being able to activate while the game is paused if the time between effects config value is changed

* Added Effect: Teleport to Random Location
* Added Effect: Activate Random Equipment
* Added Effect: Change Difficulty

**0.6.0 Changes:**

* Added Effect: Increase Chest Prices
* Added Effect: Decrease Chest Prices
* Added Effect: Spawn Void Implosion
* Added Effect: Launch Everyone in Random Directions
* Added Effect: Touch Void
* Added Effect: Duplicate Random Item Stack

* Drop All Items: Decreased chance of the effect happening many times per run

* Added config entries for effect weight reduction per activation (decrease likelyhood of effect activating many times)

**0.5.0 Changes:**

* Added Effect: Potrolling
* Added Effect: Wet Floor

* Fixed changing the time between effects config mid-run not applying properly

**0.4.0 Changes:**

* Added Effect: Warbanner
* Added Effect: Spawn Doppelganger

* Give Random Item: Fuel Cell and Elegy of Exctinction have been added the the equipment pool

* Effects giving equipment will now prioritize the active equipment slot first, then continue looking for empty ones. If no empty slots are found, the current equipment slot is overriden (old equipment is dropped at the player's feet)

* Decreased likelyhood of Gravity-based effects happening several times per stage

* Fixed +50% Director Credits not properly applying more than once per stage

**0.3.0 Changes:**

* Added effect: Corrupt Random Item

* The mod now requires every player to have the mod installed in multiplayer
  * This will make it much easier to add new (and more complex) effects in the future.

* Fixed Gravity effects not applying properly to non-host players.

* Fixed Enable Random Artifact not immediately applying health and damage stat changes when Artifact of Glass was selected

* Fixed Randomize Loadout only giving default skills and skins

**0.2.0 Changes:**

* Added effect: Give Random Elite Aspect
* Fixed Randomize Loadout forcing players out of the intro pod
* Fixed Randomize Loadout giving players skills or skins they didn't have unlocked
* Give Random Item: If giving equipment, it will now search all equipment slots for an empty one instead of just the active one, and if none are found, the equipment will be dropped at the players feet instead.
* Payday: Added config options to control how much money is given and if it should scale the amount given with interactible prices

**0.1.8 Changes:**

* Removed (now unnecessary) R2API.Networking dependency
* Fixed language tokens not loading due to invalid folder structure in last upload (oops)

**0.1.7 Changes:**

* Added effect: +50% Gravity
* Added effect: -50% Gravity
* Fixed +50% Director Credits carrying over to future stages (would still apply to directors after stage load)

**0.1.6 Changes:**

* Fixed language tokens not loading (for real this time)

**0.1.5 Changes:**

* Added effect: Scrap Random Item
* Randomize Loadout will no longer revive dead players

**0.1.4 Changes:**

* Added effect: Mitosis
* Use R2API split assemblies

**0.1.3 Changes:**

* Added effect: You and a super intelligent Lemurian...

**0.1.2 Changes:**

* Fix Randomize Loadout spawning players as a new survivor if Artifact of Metamorphosis was enabled.
* (Hopefully) fix language tokens sometimes not loading properly

**0.1.1 Changes:**

* README update

**0.1.0 Changes:**

* First release