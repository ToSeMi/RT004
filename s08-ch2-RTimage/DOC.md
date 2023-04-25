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
