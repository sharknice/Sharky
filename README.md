# Sharky
A C# framework for developing StarCraft 2 AI bots

The goal of Sharky is to provide a framework that handles all the complex setup and mundane details of creating a bot so you can focus on the fun stuff like builds, strategies, and micro.  

You can clone it and use it directly in your solution (recomended), or install it to your project via the nuget package 'Sharky' (not updated as frequently)

There is an example bot included in the Sharky source, and another example using the nuget package here https://github.com/sharknice/SharkyExampleBot

And a video tutorial here https://www.youtube.com/watch?v=2Tf0jLTJQME

Make sure you have the ladder maps installed. Download them here https://aiarena.net/wiki/maps/ and then put them in your C:\Program Files (x86)\StarCraft II\maps folder.

## Features
- Works with every race, Terran, Protoss, Zerg, and Random
- Supports Multiple builds, with the ability to intelligently switch builds on the fly
- Fast, frames are completed in the single digit miliseconds, utilizes multithreading
- Fully customizable - use or don't use any feature you want, and easily add your own
- Humanlike chat - add json files to data/default to respond to player chat, add json files to data/type to respond to enemy strategies, announce your attack, or anything you want.  You can even connect it to the sharkbot chat api https://github.com/sharkson/sharkbot
- Indentifies enemy strategies so you can counter them
- Saves game results to json files that contain the builds used and the enemy strategies used as well as the chat, these files are used to determine what build to use the next game
- Spawn friendly or enemy units in debug mode with chat commands.  Ex. type "spawn 10 enemy protoss_zealot" to spawn 10 enemy zealots at the current camera location or "spawn friendly terran_reaper" for a friendly reaper unit.  List of units here: https://github.com/sharknice/Sharky/blob/master/Sharky/S2ClientTypeEnums/UnitTypes.cs
- Turbo Mining - mines minerals over 10% faster than humanly possible by individually microing each worker
- And much more!

## Architecture
 ### Managers
- The bot has a list of Managers. Each manager is responsible for updating something, there's a UnitManager, BuildManager, MicroManager, MapManager, TargetingManager, etc.
- The managers run on each frame and return commands that are sent to the API.  They also have functions OnStart for the beginning of the game and OnEnd for the end of the game to gather and save data.
### Builds
- The BuildManager handles the builds. At the start of the game it uses the BuildDecisionService to determine the best build for the matchup.
- Builds are setup using BuildSequences which are a list of builds to execute in order.  Once a build is ready to transition to the next step in the sequence it will return true with the Transition function and the BuildManager will switch to the next build.  
- The CounterTransition function can be used to transition to a sepcfic build sequence based on enemy strategies.  This would be a good place to implement machine learning.
- Builds are run on every frame and update the desired buildings, units, upgrades, etc. which the MacroManager uses to issue build commands.
### Micro
- The MicroManager has a list of MicroTasks that run on every frame ordered by priority
- A list of all units is passed into each MicroTask and the task will claim it's desired units
- There are also MicroTasks for mining, base defense, harassing, chrono boosting, larva injection, and much more.  MicroTasks can be enabled or disabled at any time.  They're typically always enabled, or triggered in specific builds.
- The AttackTask uses IndividualMicroControllers to issue orders attack, retreat, and idle orders to army units.  It has a list of IndividualMicroControllers, each type of unit can have it's own microcontroller to behave differently, if it doesn't it will use the default IndividualMicroController
- The AttackTask uses AttackData to determine when to attack or retreat.  By default AttackData is modified by the AttackDataManager which calcuates if you should attack based on the enemy army and our own units.  It can be customized to be controlled by the build or any way you want.
- Each controllable unit has a UnitCommander which can be issued commands and determine if a command is off cooldown, prevent api overload by spamming of the same command, etc. The list of UnitCommanders is maintained by the UnitManager. 
### UnitCalculation
- The UnitManager maintains a list of every unit in the game and calculates information such as enemies in range, allies in range, enemies in range of, damage, the target priority a unit should take based on if the nearby battle is winnable, etc.  
### Pathing
- A* pathing is used for specific unit micro, but because it is slower most micro just uses the built in SC2 pathing.  
- The MapDataMangaer updates the map grid every frame with information such as enemy air dps, enemy ground dps, enemy detection, our vision, walkable area, etc.  This can be used by the pathing, or just as data to determine if it's a good area to attack, if an area should be scouted, or whateveer you want.
