# Documentation

# Checkpoint 1
## Reading input

The program can either read a specified json file as config, reading the parameters from the command line. If there are no arguments, the program will check if there is not a file named "conf.json" and reads it if exists. If not, the default parameters are set.

\[program name\]  
\[program name\] \[name of json config file\]  
\[program name\] \[width\] \[height\] \[output filename\]  

## Output

Program's output is a .pfm image, it should be a blue curve separating a black part of the image and the dark red part.


# Checkpoint 2

Basically every object in the scene is inheriting the ObjectOnMap, which contains float information about position.

## Solid

Abstract class for objects on the screen. Has also information about material.

Inherits:
- ObjectOnMap

Methods:
- Intersect(Position, Direction, out t) 

## LightSrc

Abstract class for the Light sources

Methods:
- ComputeLightContrib(model,normal, light, view)

## Image Synthetizer

Basic renderer of the image.

Methods:
- RenderImage()
- AddSolid(solid)
- AddLight(light)

## FloatCamera

Implementation of a simple perspective camera with Position (inherited from ObjectOnMap) and direction.

Methods:
- RenderPixel()
- SetCamera()

## Phong

Representation of Phong's shading model.

Methods:
- DiffuseComponent(Intensity, normal, light)
- SpecularComponent(Intensity, unitR, unitV)
- AmbientLight()
- ctor(color, kD, kS, kA)


# Checkpoint 3

## Scene

Scene is a simple representation of a scene, with lights, objects and background color. The objects are sorted by the distance from the watcher. 

Methods:
- AddSolid(solid)
- AddLight(light)

## Solid

Abstract class for objects on the screen. Has also information about material.

Inherits:
- ObjectOnMap

Methods:
- Intersect(Position, Direction, out t) 

## LightSrc

Abstract class for the Light sources

Methods:
- ComputeLightContrib(model,normal, light, view)

## Image Synthetizer

Basic renderer of the image.

Methods:
- RenderImage()


## FloatCamera

Implementation of a simple perspective camera with Position (inherited from ObjectOnMap) and direction. Casts rays and traces them in some depth stated by the user.

Methods:
- RenderPixel(int x, int y, int height, int width, ref int numOfCasts - just for debugging)
- SetCamera()
- Vector3 RayTrace(Vector3 position, Vector3 ray, ref int numOfCasts, depth = 0,currentSolid)
- refract(light, normal, outIndexRefract - index of refraction of the outside)
- reflect(normalVector, viewVector) 
- isShadowed(position, direction,currentSolid) - performs basic shadow casting 

## Phong

Representation of Phong's shading model. Contains the refractive index.

Methods:
- DiffuseComponent(Intensity, normal, light)
- SpecularComponent(Intensity, unitR, unitV)
- AmbientLight()
- ctor(color, kD, kS, kA)
- schlickApprox(V, H, l, n)
- isReflective()
- isRefractive()
- Shade(viewVector, normalVector) - computes the approximation of fresnel equations