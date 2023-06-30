# INFOGR Rasterizer; Assignment 2

## Team members
- Tobias de Bruijn (4714652)

## Implemented features

### Minimum
- Camera control
  - Keyboard controls using WASD, Space and Shift (Translation)
  - Mouse control for both yaw and pitch (Rotation)
- Scene graph
  - Infinite depth
  - Scene objects may be children of other scene objects
  - Unique texture and material per object
- Shaders
  - Full phong shading model
    - Ambient lighting
    - Diffuse lighting,
    - Specular highlights
- Demonstration scene
  - Please enjoy the Teapot Party.

### Extra bonus assignments
- Multiple lights
  - Supports up to 5 lights (Though this can easily be increased)
- Frustrum culling
  - Implemented with some degree of success. Not successful enough though, so it is disabled by default,
    but can be enable in SceneGraph.cs L56.

### Extra features not mentioned in the assignment
- Objects can have a diffuse tint. See the disco teapots.

## Used resources
- Slides from the INFOGR course
- [Project for Assignment 1 (Raytracer)](https://github.com/TobiasDeBruijn/UUGraphicsRaytracer)
- [OpenGL Wiki](https://www.khronos.org/opengl/wiki/)