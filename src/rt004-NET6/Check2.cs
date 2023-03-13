using System.Collections.Generic;
using Util;
using System.Numerics;

namespace rt004.checkpoint2 {
	
	public class Scene {
		FloatImage fi;
		List<Solid> objects;
		List<LightSrc> lights;
		FloatCamera camera;
		string filename;

		public Scene(int wid, int hei, string filename, FloatCamera fc) {
			fi = new FloatImage(wid, hei, 3);
			this.filename = filename;
			objects = new();
			lights = new();
			this.camera = fc;
		}

		public void AddSolid(Solid s) {
			objects.Add(s);
		}

		public void AddLight(LightSrc light){
			lights.Add(light);
		}	

		public Scene? LoadScene(string fileName){
			Scene? returnable = null;

			return returnable;
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
		public abstract void Draw(FloatCamera fc);

		public abstract bool Intersect(FloatCamera fc);
	}

	public abstract class LightSrc  {
		protected Vector3 Intensity;
		protected Phong? model;
	}

	public class PointLightSource : LightSrc {

	}

	public class DirectionLightSource : LightSrc {
		Vector3 Direction;
		public void shade() {
			var E = //model?.AmbientLight() 
			  model?.DiffuseComponent() 
			+ model?.SpecularComponent(Vector3.UnitX, Vector3.UnitY); 
		}
	}
	/*
	I want to have a perspective camera
	*/
    public class FloatCamera : ObjectOnMap {
		Matrix4x4 projection;
        Vector3 direction;
		float frustrum;

		public Vector3 Direction {get => direction; set=> position = value;}
		public float Frustrum {get=>frustrum; set=> frustrum=value;}

		public FloatCamera(float x = 0,float y = 0, float z = 0, float a1 =0, float a2=0, float a3 =0, float frustrum =0){
			this.direction = new Vector3(x,y,z);
			this.position = new Vector3(a1,a2,a3);
			this.frustrum = frustrum;
			projection = new Matrix4x4(	1,0,0,0,
										0,1,0,0,
										0,0,1,1/frustrum,
										0,0,0,0);
		}
		public FloatCamera(Vector3 pos, Vector3 dir, float frustrum = 0){
			this.position = pos;
			this.direction = dir;
			this.frustrum = frustrum;
		}


    }

    public class Plane : Solid
    {
        public override void Draw(FloatCamera fc)
        {

			if(Intersect(fc)){
				var D = Vector3.Distance(fc.Position,this.Position); //D stands here for distance

				var p0 = fc.Position;
				var p1 = fc.Direction;
				var n = Position; //i take position here as normal
            	var t =-1*(Vector3.Dot(n , p0) + D) / (Vector3.Dot(n,p1));
				var pt = p0 + t*p1; //parametric intersection
			}
        }

        public override bool Intersect(FloatCamera fc)
        {
			var D = Vector3.Distance(fc.Position,this.Position);

			var p0 = fc.Position;
			var p1 = fc.Direction;
			var n = Position;
			if((Vector3.Dot(n,p1)) <= 10e-9)
				return false;
			
			return true;
			
        }

    }

    public class Sphere : Solid {
        public override void Draw(FloatCamera fc)
        {
            var P0 = fc.Position;
			var P1 = fc.Direction;
			var D = Math.Pow(Vector3.Dot(P1,P0),2) - 4*(Vector3.Dot(P1,P1)*Vector3.Dot(P0,P0));
			var t0 = (float)(-1*Vector3.Dot(P1,P0) + Math.Sqrt(D)) / (2*Vector3.Dot(P1,P0));
			var t1 = (float)(-1*Vector3.Dot(P1,P0) - Math.Sqrt(D)) / (2*Vector3.Dot(P1,P0));  
			var pt0 = P0 + t0*P1;
			var pt1 = P0 + t1*P1;
        }

        public override bool Intersect(FloatCamera fc)
        {

			//t^2(P1.P1) + 2t(P1.P0) + (P0.P0) - 1 = 0
			var P0 = fc.Position;
			var P1 = fc.Direction;
			var D = Math.Pow(Vector3.Dot(P1,P0),2) - 4*(Vector3.Dot(P1,P1)*Vector3.Dot(P0,P0));
			if(D < 0)
				return false;
			return true;
			
        }

    }

}