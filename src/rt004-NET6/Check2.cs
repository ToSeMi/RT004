using System.Collections.Generic;
using Util;
using System.Numerics;

namespace rt004.checkpoint2 {
	
	public class ImageSynthetizer {
		List<Solid> objects;
		List<LightSrc> lights;
		FloatCamera camera;
		string filename;
		int width, height;

		public ImageSynthetizer(int wid, int hei, string filename, FloatCamera fc) {
			this.filename = filename;
			objects = new();
			lights = new();
			this.camera = fc;
			this.width = wid;
			this.height = hei;
		}

		public void AddSolid(Solid s) {
			objects.Add(s);
		}

		public void AddLight(LightSrc light){
			lights.Add(light);
		}	
		public FloatImage RenderScene() {
			FloatImage fi = new FloatImage(width, height, 3);
			Vector3 center = camera.Position;
			Vector3 direction = camera.Direction;
			float d = 60;
			Vector3 u = Vector3.UnitY;
			foreach(var solid in objects){
				var t = solid.CalculateT(camera);
				var pt = center + t*direction;
				Vector3 color = new Vector3();
				if(t >= 10e-9){ 
					foreach(var light in lights){
						float contrib = light.ComputeLightContrib(solid);
						color += (contrib * pt);
					}
				}else {
					color += new Vector3(1,1,1);
				}

			}
			
			return fi;
		}
	}

	public class Phong {
		Vector3 normal,light,view, color;
		public readonly float H = 0.5479f;
		public readonly float kA = 0.3f, kD = 0.3f, kS = 0.4f; // kA + kD + kS = 1


		public Phong(Vector3 n, Vector3 l, Vector3 v, Vector3 color, float highlight,float kA, float kD, float kS){
			normal = n; 
			light = l;
			view = v;
			this.kA = kA;
			this.kD = kD;
			this.kS = kS;
			this.H = highlight;
			this.color = color;
		}
		public float DiffuseComponent( ){
			return  Vector3.Dot(normal,color)*kD * Vector3.Dot(view,normal);
		}
		public Vector3 AmbientLight(){
			return color*kA;
		}
		public float SpecularComponent(Vector3 unitV, Vector3 unitR){
			return Vector3.Dot(light,color)*kS*(float)Math.Pow(Vector3.Dot(unitR,unitV),H);
		}
		
	}
	public abstract class ObjectOnMap {
		protected Vector3 position;
		public Vector3 Position {get => position; set=> position = value;}
	}
	public abstract class Solid : ObjectOnMap {
		public Phong? Model {set;get;}
		public abstract float CalculateT(FloatCamera fc);

		public abstract bool Intersect(FloatCamera fc);
	}

	public abstract class LightSrc : ObjectOnMap  {
		protected Vector3 Intensity;
		public abstract float ComputeLightContrib(Solid s);
	}


	public class DirectionLightSource : LightSrc {
		public Vector3 Direction;

		public DirectionLightSource(Vector3 dir, Vector3 intensity) {
			this.Direction = dir;
			this.Intensity = intensity;
		}

		public override float ComputeLightContrib(Solid s) {
			var E = //model?.AmbientLight() 
			  s.Model!.DiffuseComponent() 
			+ s.Model!.SpecularComponent(Intensity, this.Direction); 
			return E;
		}
	}
	/*
	I want to have a perspective camera
	*/
    public class FloatCamera : ObjectOnMap {

        Vector3 direction;
		float frustrum;
		public FloatImage image;
		int dimension;

		public Vector3 Direction {get => direction; set=> position = value;}
		public float Frustrum {get=>frustrum; set=> frustrum=value;}

		public FloatCamera(FloatImage fi, float x = 0,float y = 0, float z = 0, float a1 = 0, float a2 = 0, float a3 = 0, float frustrum = 0, int dim = 0){
			this.direction = new Vector3(x,y,z);
			this.position = new Vector3(a1,a2,a3);
			this.frustrum = frustrum;
			
			this.image = fi;
			this.dimension = dim;
		}
		public FloatCamera(FloatImage fi, Vector3 pos, Vector3 dir, float frustrum = 0){
			this.position = pos;
			this.direction = dir;
			this.frustrum = frustrum;
			this.image = fi;
		}

    }

    public class Plane : Solid
    {
		public Plane(Vector3 pos, Phong model){
			this.position = pos;
			this.Model = model;
		}
        public override float CalculateT(FloatCamera fc)
        {

			if(Intersect(fc)){
				var D = Vector3.Distance(fc.Position,this.Position); //D stands here for distance

				var p0 = fc.Position;
				var p1 = fc.Direction;
				var n = Position; //i take position here as normal
            	var t =-1*(Vector3.Dot(n , p0) + D) / (Vector3.Dot(n,p1));
				var pt = p0 + t*p1; //parametric intersection
				return t;
			}return 0;
        }

        public override bool Intersect(FloatCamera fc)
        {
			var p0 = fc.Position;
			var p1 = fc.Direction;
			var n = Position;
			if((Vector3.Dot(n,p1)) <= 10e-32)
				return false;
			
			return true;
			
        }

    }

    public class Sphere : Solid { 
		public Sphere(Vector3 pos, Phong model){
			this.position = pos;
			this.Model = model;
		}
        public override float CalculateT(FloatCamera fc)
        {
			//t^2(P1.P1) + 2t(P1.P0) + (P0.P0) - 1 = 0
			if(Intersect(fc)){
            	var P0 = fc.Position;
				var P1 = fc.Direction;
				var a = Vector3.Dot(P1,P1);
				var b = Vector3.Dot(P1,P0);
				var c = Vector3.Dot(P0,P0) -1;
				var D = Math.Pow(b,2) - 4*(a*c);
				var t0 = (float)(-1*b + Math.Sqrt(D)) / (2*a);
				var t1 = (float)(-1*b - Math.Sqrt(D)) / (2*a);  
				var pt0 = P0 + t0*P1;
				var pt1 = P0 + t1*P1;
				return t0;
			}
			return 0;
        }

        public override bool Intersect(FloatCamera fc)
        {

			var P0 = fc.Position;
			var P1 = fc.Direction;
			var a = Vector3.Dot(P1,P1);
			var b = Vector3.Dot(P1,P0);
			var c = Vector3.Dot(P0,P0) -1;
			var D = Math.Pow(b,2) - 4*(a*c);
			if(D < 0)
				return false;
			return true;
			
        }

    }
	
}
