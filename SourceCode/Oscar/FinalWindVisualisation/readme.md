The following explains how to generate streamline CSV files from ParaView, and how to setup the Unity project so that those generated files can be used to represent wind-flow.

##### 

##### **Generating Streamlines**



Firstly, you need to have streamlines generated from *Paraview*, as they are required for this visualisation. 

Timesteps from 30 - 40 are already provided in the GitHub repo. 





To generate the streamlines yourself: 

1. Download the *'visualize.pvsm*' file from the repo and place in the same folder where your *Stage3.foam* is located. 
2. Open *ParaView* and once it has loaded click on the *File* tab in the top-left corner and from the drop-down select *load state*. 
3. From here locate and select the downloaded state named *visualize.pvsm*. 
4. Once it has loaded you can change the number of streamlines to visualise by selecting *StreamTracer1* in the *Pipeline Browser* and in *Properties* you can change the *Number Of Points*, by default this is set to 150.
5. To extract the streamlines select *Connectivity* in the *Pipeline Browser*. With it selected click on *File* and select *Save Data*, when saving data it is important that you name them in a way that they will be ordered accordingly, i.e. 30, 31, 32,...
6. Ensure that the data is saved as a *.csv* file. When in *the Configure Write* window select *Choose Arrays To Write* and from the dropdown ensure that only the first ***RegionId*** is selected and ***CasePath*** is selected, to find *CasePath* you may have to scroll down.







##### **Setting up Unity**



The files in the repo have been structured to reflect the recommended file structure within the Unity project. 





1. Either create or open an already existing project in Unity.
2. From the repo download the contents of the files named *'Materials'*, *'Prefabs'*, *'Resources/Streamline'* (Unless you've generated streamlines from 30 - 40 yourself), and *'Scripts'*.
3. Once downloaded go into Unity and place all files within the *Project* window into their respective folders. You can create a folder in the *Project* window by right clicking in the *P*roject window and *C*reate followed by *New Folder*. Alternatively, you can also locate the project in your file explorer and add folders or files that way. **The streamline CSV files must be placed under *'Resources/Streamline'***.
4. Locate the *'Wind'* prefab within the project view and click it once. You should now see it selected in the *Inspector* window, located to the right of the scene view.
5. Within the *Inspector* window open the *Trail Renderer* component. Once open, scroll down until you see an option labelled *Materials* and expand it by clicking it. With it open, locate the material labelled *'trail'* and drag it into *Element 0*.
6. In the *Hierarchy* window, located to the left of the *Scene* view, right click in an empty space and select *Create New Empty* and name *it 'StreamlineSystem*'. Do this once more except name the object *'Particles'*.
7. Select *StreamlineSystem* from the *Hierarchy* window, you should now see it in the *Inspector* window. Locate the scripts called *'CombinedStreamlineSystem'* and *'ForwardStreamlineSystem'*. Drag them each from the *Project* window into the *Inspector* below *Add Component*.
8. Within both script components drag and drop the *Wind* prefab into the variable named *'Particle Prefab'*. Then drag the *Particles* object, from the *Hierarchy* window, into the variable of each object labelled *'Particle Parent'*.
9. Before running the project make sure that only one script is ticked, the checkbox can be found to the left of the respective script's name. The *Combined Streamline System* visualises the entire streamline, whereas the *Forward Streamline System* visualises only the second half of the streamline.
10. Finally, you can run the project by selecting the play button above the *Scene* view. Once it is playing, you can select the *Scene* tab to view the visualisation in real-time. The most flexible way to explore the scene is to hold down right click and use WASD to move up, left, down, and right. You can also move immediately up by holding E or down by holding Q.







##### **Additional Changes**



As mentioned in the first section, you can generate any number of streamlines per time step to be visualised within Unity. 

The trail material can be changed however you like, by selecting it from the *Project* view and altering it in the *Inspector* window. Such as changing it to be opaque rather than transparent or changing the colour.

The scripts in the *StreamlineSystem* allow you to change the speed at which the particles travel, or the particles that will spawn per a streamline. These can be changed real-time within the scene view, provided the *StreamlineSystem* is selected in the *Inspector* window.

You can also change the *File Switch Interval* which either increases or decreases the time between the next streamline being loaded.







