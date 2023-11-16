# About
**Tested on Unity 2020.3.5. Should work on older and newer versions supporting .NET 4.6.**

Vertex Lightmapper is a Unity Tool which allows lighting information to be baked into the vertex colors of meshes. A major goal of this tool is for it to feel as natively integrated into Unity as possible.

This is the tool that was developed and has been continiously evolved for use in BallisticNG, minus the baking for the games TRM mesh format. A seperate folder for shaders and a simple API to control global shader uniforms is included.

### Vertex Lightmapper Features
* Bakes vertex colors onto meshes inside of Unity, applying directly to the mesh or as an [Additional Vertex Stream](https://docs.unity3d.com/ScriptReference/MeshRenderer-additionalVertexStreams.html)
* Can bake lighting information into mesh tangents to allow the vertex colors to be used by other tools
* Uses native Unity lights and ambient settings, plus two extras for added functionality (Light Sponges and GI Areas)
* Per-scene bake settings

### Shader Features
* Two uber shaders with support for a large variety of vertex lit setups and effects, including retro effects.
* A tri-planar mapped terrain shader with RGBA texture blending (reads lighting information from mesh tangents).

# Examples
Check out the `City Day`, `City Night` and `Terrain` scenes for some examples!

# How to Use
Note: Turn off MSAA if you're planning to use the included shaders!

[Check the Wiki page](https://github.com/bigsnake09/Vertex-Lightmapper-2/wiki)
