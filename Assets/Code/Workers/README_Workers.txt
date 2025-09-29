WORKER SYSTEM + GHOST BUILDING SETUP
===================================

This system provides AI workers that build ghost buildings placed by the player.

QUICK SETUP:
-----------
1. Open your main scene (Assets/Scenes/Game.unity)
2. Find the "System" GameObject in the hierarchy
3. Add the "WorkerSystemSetup" component to it
4. In the WorkerSystemSetup component:
   - Set "Worker Prefab" to Assets/Prefabs/Worker.prefab
   - Enable "Create Job Manager", "Create Worker Manager", etc.
   - Configure spawn settings as needed
5. Play the scene - workers should spawn automatically

HOW IT WORKS:
------------
- When you place buildings (walls, floors, doors), ghost versions appear
- Workers automatically get assigned to build these ghost structures
- Workers move to the construction site and gradually build the structure
- Once complete, the ghost becomes a real building

CONTROLS:
--------
- Number keys (1,2,3) to select building types
- Left click to place ghost buildings
- Right click to cancel/demolish
- Press 'T' to manually spawn additional workers

TESTING:
--------
1. Start the game
2. Select a building type (press 1 for floor, 2 for wall)
3. Click to place - you'll see ghost buildings appear
4. Watch workers automatically move to build them
5. Buildings will gradually construct and become solid

COMPONENTS:
----------
- Worker.cs: AI behavior with job handling
- WorkerManager.cs: Manages worker spawning and tracking  
- JobManager.cs: Assigns construction jobs to workers
- GhostBuilding.cs: Ghost buildings that need construction
- BuildPlacer.cs: Modified to create ghost buildings

CURRENT FEATURES:
----------------
- Ghost building system with transparent sprites
- Automatic job assignment to closest available workers
- Building progress visualization
- Workers pathfind to construction sites
- Completed buildings replace ghosts automatically
- Job cancellation when buildings are demolished

STATE VISUALIZATION:
------------------
Worker gizmo colors when selected:
- Green: Idle (wandering around)
- Blue: Moving (general movement)
- Yellow: Moving to job
- Red: Working (building)

DEBUG INFO:
----------
- Use WorkerTester component for manual testing
- Job Manager shows pending/active job counts in top-right
- Console logs show worker assignments and building completion
- Gizmos show worker targets and build progress when selected

PREFAB LOCATION:
---------------
Worker prefab: Assets/Prefabs/Worker.prefab
- Brown/orange colored square sprite
- Has Worker, Rigidbody2D, and CircleCollider2D components
