To generate the widn CSVs use the visualizeWind state in Paraview. Save the Connectivity1 Filter as data with the selected arrays: IntegraitonTime, RegionId, U and CasePath.

Once the script, prefab, material and streamline CSV is in Unity.

Create 2 empty objects in the scene hierarchy (panel on the left), naming one StreamlineSystem and the other Particles.

Select the StreamlineSystem from the hierarchy panel and drag the StreamlineSystem script into the inspector (panel to the right once) to add it as a component.

Select the prefab named 'Wind' and in the inspector window scroll down until you see the TrailRenderer component. Under the component there will be a drop-down box titled 'Material', open the drop-down box and set Element0 to the 'trail' material.

After completing the previous steps select the StreamlineSystem in the hierarchy and within the component named StreamlineSystem, in the inspector panel:
Drag in the wind streamline CSV file to the variable labelled Csv File.
Drag the wind prefab into the variable labelled Particle Prefab.
Drag the Particles object from the scene hierarchy into the Particle Parent.
Ensure that Speed is set to atleast 100.
