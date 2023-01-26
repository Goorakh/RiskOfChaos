## Changelog

**(Next Version) Changes:**

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