# MonoLandscape

## Features
- [ ] Non-destructive landscape editing
- [x] LOD0-LOD5 support

## The Importer
When you select the MonoTerrain node, you can see the
importer button on top on the scene view. Click it and
follow the instructions to import data from a single heightmap.

## Technical Details
### Region, as the base unit
A MonoTerrain Node is divided into several regions, each region consists patches (a n*n mesh chunk). Each chunk is a 16x16 (customizable) grid of vertices.
Loading textures and building quadtree is also based on each region.

Since the system support LOD0-5 (6 levels in total), you can get
the scale of the region by `2^(LOD-1) * patchSize`.

After building the quadtree, the region will load the maximum required level of
detail data into memory.