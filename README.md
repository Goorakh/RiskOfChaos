# Risk of Chaos

Every minute, a random effect happens. Multiplayer combatible! Every player needs to have the mod installed.

Heavily inspired by the "Chaos Mod" series of GTA games.

Currently includes 26 different effects.

<details>
	<summary>Full list of effects</summary>

* Nothing: Does absolutely nothing
* Spawn Random Portal Orb: Spawns a random portal orb on the stage teleporter
* Enable Random Artifact: Enables a random artifact. Lasts until end of stage.
* Add Mountain Shrine: Adds a number of mountain shrines to the teleporter event, number of shrines added can be configured (default is 2)
* Activate Teleporter: Forcefully activates the stage teleporter, whether you want it to or not
* Give Random Item: Gives all players a random item
* One Hit KO: Sets everything on the stage to 1 HP
* Freeze!: Freezes every character on the stage for 4 seconds
* Payday: Gives all players an amount of money equivalent to 4 large chests. (Amount can be configured)
* +50% Director Credits: Increases director credits for the rest of the current stage
* Sequence All Players: Activates a shrine of order on all players
* Spawn Scavenger Bag: Spawns a scavenger bag near a random player
* Drop All Items: Drops all players' items and equipment on the ground
* Meteor Shower: Activates the glowing meteorite equipment
* Randomize Loadout: Randomizes all player's loadouts (skills and skins)
* You and a super intelligent Lemurian...: Spawns an invincible Lemurian in a random location on the map with an infinite damage stat (instantly die if you touch it)
* Mitosis: Duplicates every character on the map
* Scrap Random Item: Turns a random scrappable item in every players inventory into the corresponding scrap
* Increase Gravity: Increases gravity by a configurable amount (default +50%), lasts until the end of the current stage
* Decrease Gravity: Decreases gravity by a configurable amount (default -50%), lasts until the end of the current stage
* Give Random Elite Aspect: Gives all players a random elite aspect (drops on the ground if they don't have any empty equipment slots)
* Corrupt Random Item: Converts a random item in every player's inventory to the void variant
* Warbanner: Spawns a warbanner on every character
* Spawn Doppelganger: Triggers the Artifact of Vengeance event
* Potrolling: Spawns a bunch of nice pots for you to roll
* Wet Floor: Every surface is slippery. Lasts until end of current stage
</details>

FAQ:

Q: The icon looks like shit<br/>
A: That's not a question

Q: Why does the icon look like shit?<br/>
A: I failed art class __(If you have a better design in mind please let me know, I'll take any excuse to get rid of that abomination)__

If you have any feedback or bug reports, please open a [GitHub issue](https://github.com/Goorakh/RiskOfChaos/issues/new)

## Changelog

**(Next Version) Changes:**

* Added Effect: Potrolling
* Added Effect: Wet Floor

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
