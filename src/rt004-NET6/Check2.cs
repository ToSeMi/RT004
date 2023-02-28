using System.Collections.Generic;
using Util;
using System.Numerics;

namespace rt004.checkpoint2 {
	
	class Scene {
		FloatImage? fi;
		List<Solid> objects;
		FloatCamera camera;
		string filename;

		public Scene(int wid, int hei, string filename, FloatCamera fc) {
			fi = new FloatImage(wid, hei, 3);
			this.filename = filename;
			objects = new();
			this.camera = fc;
		}

		public void AddSolid(Solid s) {
			objects.Add(s);
		}

		public Scene? LoadScene(string fileName){
			Scene? returnable = null;

			return returnable;
		}
	}

	abstract class ObjectOnMap {
		protected Vector3 position;
		protected Scene? scene;
		public abstract void Draw();
	}
	abstract class Solid : ObjectOnMap {
		protected Vector2 TxtCoords;
		public override void Draw() {
			throw new NotImplementedException();
		}

		public abstract bool Intersect(FloatCamera fc);
	}

	class LightSrc : ObjectOnMap {
		public override void Draw(){
			throw new NotImplementedException();
		}
	}
	/*
	I want to have a perspective camera
	*/
    class FloatCamera : ObjectOnMap {
        Vector3 direction;
		float frustrum;

		public FloatCamera(float x = 0,float y = 0, float z = 0, float a1 =0, float a2=0, float a3 =0, float frustrum =0){
			this.direction = new Vector3(x,y,z);
			this.position = new Vector3(a1,a2,a3);
			this.frustrum = frustrum;
		}
		public FloatCamera(Vector3 pos, Vector3 dir, float frustrum = 0){
			this.position = pos;
			this.direction = dir;
			this.frustrum = frustrum;
		}
        public override void Draw()
        {
            throw new NotImplementedException();
        }

    }
	
	class Sphere : Solid {
		float radius;

        public override bool Intersect(FloatCamera fc)
        {
            throw new NotImplementedException();
        }
    }
	//geometric tools
	class Cube : Solid {
		float side;

        public override bool Intersect(FloatCamera fc)
        {
            throw new NotImplementedException();
        }
    }
}