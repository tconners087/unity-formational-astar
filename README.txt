
# unity_formational_astar

There is a method that is being called in the AstarUser.cs
script that instantiates GameObjects with cube meshes to represent each AstarNode,
enables each instantiated object, and colors their materials based on
several flags pertaining to their purpose in the A* search.

Each one of these meshes casts shadows. I'm running Unity with
an Nvidia GTX 1050ti, so this did not cause problems for me,
but I don't know if it will run well with older hardware.

To disable these objects, comment out line #34 of the
AstarUser.cs script, which reads:

"controller.visualize(roomNodes);"

The invisible leader is running A* and moving along a 
generated path using a movement coroutine ("move") in the
AstarUser script. The invisible leader is moving with a movement
speed of 10.2, and the following objects are using the 
KinematicArrive.cs script with a speed of 10. This is to smooth
the movement of the followers, but it works if the speeds are
identical, too. To change the speed of the invisible leader, 
set the "speed" float of the AstarUser script attached to the
Leader object in the inspector.

There is also a sphere mesh childed to the Leader object that will
show the Leader moving along the path if enabled. The follower
in the most forward position of the formation follows very closely
behind the leader, though, so it isn't really necessary.

Left click moves, right click spawns or destroys obstacles.
