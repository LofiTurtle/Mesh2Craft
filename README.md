## Mesh2Craft

This is a mod for SimpleRockets 2 that rebuilds 3D models (.obj files) using stock structural panels. Models must be triangulated. Each face becomes a structural panel, and they are all connected to a signle root part.

Usage is straightforward, there is one screen in the Designer with all the options. Obj files need to go in `SimpleRockets 2\UserData\Mesh2Craft\Models\`. The craft needs to be saved before importing a model, and a new craft file will be created. The `Models` folder has some test models to use.

## TODO

* Option to auto-select root when a face is selected.
* Ability to use fuel tanks instead of structural panels, to make small models easier to work with.
* Possibly support wireframe models using cylindrical fuel tanks.

## Known Issues

* Attachment points make it difficult to work on small models with high shell widths.