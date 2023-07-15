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