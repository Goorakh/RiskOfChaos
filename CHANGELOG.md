## Changelog

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