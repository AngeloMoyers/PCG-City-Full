Controls:
In Free-cam:
WASD - Move
Space - Ascend
Mouse - Turn
Q - Go to Locked Cam

In Locked Cam (Followed an agent)
A,D - Go to Prev, Next agent
Mouse - Turn
Left-click - Move Agent to location
Q - Go to Free Cam

General Notes:
All of the interesting stuff happens mainly in RoadGeneration.cs and CityBlock.cs. Startup for me averages around
40-45 seconds, and FPS between 45-60 without profiling, in editor mode. 

How It Works: 
First, a Voronoi Diagram is created. Each cell generated becomes a city District (Financial, Residential, etc.).
I Then shrink the cells in order to create gaps in between each of them, which are filled by the main roads. 
Next, these cells are "boxed in," and then the boxes are separated into grids. These grids will
contain the CityBlocks in each available tile/coord. Blocks are then separated into 4 sections, each of which
will generate a building. Building generation is dependent on the District Type. There are different sets
of prefabs that can be used for the different district types, and the buildings are generated in order from
bottom floor to top, by randomly picking a base level prefab dependent on district type, then a midlevel prefab,
and instantiating enough of the midlevels to reach the desired building height, then finally picking a roof and
placing it on top. Building colors are also assigned dependent on district type, and some district types
apply a modifier to the height of the buildings within, after using Noise to generate an initial height 
(Financial districts scale up, residential and industrial scale down). Lastly, AIAgents are spawned in, 
each receiving a home building, and a work building, which they will travel between.

Almost everything is tunable, and most tuning happens on the scripts mentioned above.

Optimization Issues, Approaches, and Concerns:
Naturally, as the project got larger, it got slower. Performance overall was pretty good up until generating 
the buildings procedurally (the previous, mostly placeholder method just spawned cubes and scaled them accordingly).
That changed drastically tanked both startup and FPS. There was some optimizing I did to bring startup times down,
but there wasn't much I could do because Instantiation has to happen on the Main Thread in Unity, and slowly spawning
the city in after launch didn't seem like a good solution. As far as FPS goes though, there were massive improvements.
As you saw, after the first implementation of the procedural buildings, frames went as low as 0.2 FPS. After a couple
of fixes to random things that shouldnt have been happening (like generating noise every frame), most of the processing
power was going to Physics updates and Rendering. For physics, I removed almost every collider possible, and left
colliders only on the base objects of the buildings (the cubes, not any of the decorations). I also have polygons being
created in the shape of the districts to check for objects that may have been spawned in them that don't belong,
and deleting them. So I made a change that after a few seconds of runtime, to disable those colliders too. After that,
pretty much all of the power was going to rendering. So first, I tried combining all of the objects involved in a single
building into one mesh. this drastically improved framerate, but slowed down startup and really dulled the city
down due to the buildings being required to be monocolored (I know there is a way to generate a texture to work 
properly, but I couldnt get it working). So next, I decided to separate buildings out by color, and then combine
all objects of the same color into a single mesh, making buildings go from a collection of hundreds of meshes
to a collection of 4 or 5 meshes. This kept framerate down a lot still, while also bringing some color and life
back to the city. I then started culling objects, as well as dropped the far camera clipping pane some. Nothing
fancy like an LOD or anything is going on with the buildings, its really just culling the extra details at a 
certain distance based on what layer they're on. 
 

Startup Time is still a bit rough, averaging around ~40-45 seconds for me, but I decided it was a necessary evil
to keep my city looking visually interesting and dynamic. In theory, the startup time could by brought way down
simply by tuning some variables and generally either shrinking the entire city itself, or making there be less
buildings in the city in general by shrinking the bounds, making blocks larger, etc. I left it the way it is
because I wanted to generate a city, not a downtown district of some village or town. 