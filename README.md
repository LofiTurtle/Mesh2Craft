## Mesh2Craft

This is a mod for SimpleRockets 2 that rebuilds 3D models (.obj files) using stock structural panels. Models must be triangulated. Each face becomes a structural panel, and they are all connected to a signle root part.

Usage is straightforward, there is one screen in the Designer with all the options. Obj files need to go in `UserData\Mesh2Craft\Models\`. The craft needs to be saved before importing a model, and a new craft file will be created. The `Models` folder has some test models to use.

## TODO

* Implement overwriting the current craft.
* Option to auto-select root when a face is selected.
* Possibly support wireframe models using cylindrical fuel tanks.

## Known Issues

* "Create New Craft" toggle is non-functional.
* When importing a model to a craft with unsaved changes, the changes will not be included in the new craft file with the model.
* Text color on the selected .obj button is hard to read.