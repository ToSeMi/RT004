using OpenTK.Mathematics;
using System.Text.Json;
namespace rt004.checkpoint2
{
    /// <summary>
    /// Since in the hiearchy everything does not need to be solid, this interface ensures we get an intersection during intersecting the scene
    /// with a ray. It can be either Solid or just a transforming vertex.
    /// </summary>
    
    public interface IIntersectable
    {
        /// <summary>
        /// Function for intersections. The function should decide from the position coordinates and direction vector whether there is and intersection or not.
        /// If there is an intersection, there is required to write the information about intersection to the hit record t.
        /// </summary>
        /// <param name="position">Position coordinates, from where the ray is starting.</param>
        /// <param name="direction">Direction vector of the ray.</param>
        /// <param name="t">Hit record. If the function returns false, it can be whatever, although I would personally settle with setting it to default.</param>
        /// <returns>Whether ray intersects any intersectable object in the scene.</returns>
        bool Intersect(Vector3d position, Vector3d direction, out Hit t);
    }

    /// <summary>
    /// Abstract class for defining scenes. Derived scene classes need to implement the IntersectScene function and that is all. I made this
    /// to create some abstraction for the scenes and to compare the scenes with and without hiearchy. So this abstraction does not really have to
    /// be here but I made it for debugging purposes.
    /// </summary>
    public abstract class AbstractScene
    {
        protected float[] backgroundColor;
        public List<LightSrc> Lights {  set; get; }
        public Vector3d BackgroundColor { get => new Vector3d(backgroundColor[0], backgroundColor[1], backgroundColor[2]); }
        public abstract bool IntersectScene(Vector3d position, Vector3d ray, out Hit info);
        public void AddLight(LightSrc light)
        {
            Lights.Add(light);
        }
    }
    public class SceneFlatRepreGraph : AbstractScene{
        private int RootIndex;
        private List<IIntersectable> listOfNodes;

        public SceneFlatRepreGraph(List<LightSrc> lights, List<IIntersectable> objects, float[] backgroundColor, int rootIndex) {
            this.Lights = lights;
            this.listOfNodes = objects;
            this.backgroundColor = backgroundColor;
            this.RootIndex = rootIndex;
        }

        public override bool IntersectScene(Vector3d position, Vector3d ray, out Hit info)
        {
            return listOfNodes[RootIndex].Intersect(position,ray,out info);
        }
    }
    public class InnerFlatVertex : IIntersectable
    {
        public List<IIntersectable> listOfNodes;
        public int[] children;
        public Matrix4d transformMatrix;

        public bool Intersect(Vector3d position, Vector3d direction, out Hit t)
        {
            Matrix4d.Invert(transformMatrix, out Matrix4d invertedMatrix);

            position = Vector3d.TransformPosition(position, invertedMatrix);
            direction = Vector3d.TransformVector(direction, invertedMatrix);
            Hit closest = default;
            double closestPosition = double.MaxValue;
            foreach (var childIndex in children)
            {

                bool possibleCandidate = listOfNodes[childIndex].Intersect(position, direction, out Hit candidate);

                if (possibleCandidate)
                {
                    var candidatePosition = Vector3d.DistanceSquared(position, candidate.point);
                    if (candidatePosition < closestPosition )
                    {
                        closestPosition = candidatePosition;
                        closest = candidate;
                    }
                }
            }
            if (closest == default)
            {
                t = default;
                return false;
            }
            closest.point = Vector3d.TransformPosition(closest.point, transformMatrix);
            closest.normal = Vector3d.TransformNormalInverse(closest.normal,invertedMatrix);
            t = closest;
            // System.Console.WriteLine(closest.material.color);
            return true;
        }
    }
    /// <summary>
    /// Hierarchic scene representation class.
    /// </summary>
    public class SceneGraph : AbstractScene
    {

        public Vertex root;

        public SceneGraph(float[] backgroundColor, Vertex root)
        {
            this.backgroundColor = backgroundColor;
            this.root = root;
            Lights = new List<LightSrc>();
        }
        /// <summary>
        /// This override uses recursion to decide whether the ray intersects the scene or not.
        /// </summary>
        /// <param name="position">Position coordinates, from where the ray is starting.</param>
        /// <param name="direction">Direction vector of the ray.</param>
        /// <param name="t">Hit record. If the function returns false, it can be whatever, although I would personally settle with setting it to default.</param>
        /// <returns>Whether the ray intersects the scene or not.</returns>
        public override bool IntersectScene(Vector3d position, Vector3d direction, out Hit t)
        {
            return root.Intersect(position, direction, out t);
        }

        public void SaveToFile(string fileName) {

        }

        public static void BuildSceneFromFile(string fileName) {
            string jsonText = File.ReadAllText(fileName);
            System.Console.WriteLine(jsonText);
        }


    }
    /// <summary>
    /// Inner vertex node of the Scene graph, which is intersected during the IntersectScene procedure. The struct is chosen because of its
    /// lower memory overhead.
    /// </summary>
    [Serializable]
    public struct Vertex : IIntersectable
    {
        public IIntersectable[] children;
        /// <summary>
        /// Transform matrix, which is used in the Intersect function when transforming the position, ray and the normal.
        /// </summary>
        public Matrix4d transformMatrix;
        /// <summary>
        /// The override of the Intersect function is implemented with the use of recursion and the transform matrix. First the function transforms the
        /// position and direction of the ray by the inverse matrix of the transform matrix and calls the Intersect function of the neighbors of current vertex. Then if there is no intersection, it ends and returns false.
        /// Otherwise the function transforms the point and normal by the matrix and returns true.
        /// </summary>
        /// <param name="position">Position coordinates, from where the ray is starting.</param>
        /// <param name="direction">Direction vector of the ray.</param>
        /// <param name="t">Hit record. If the function returns false, it can be whatever, although I would personally settle with setting it to default.</param>
        /// <returns>Whether the ray intersects the scene or not.</returns>
        public readonly bool Intersect(Vector3d position, Vector3d direction, out Hit t)
        {
            Matrix4d.Invert(transformMatrix, out Matrix4d invertedMatrix);

            position = Vector3d.TransformPosition(position, invertedMatrix);
            direction = Vector3d.TransformVector(direction, invertedMatrix);
            Hit closest = default;
            double closestPosition = double.MaxValue;
            foreach (var child in children)
            {

                bool possibleCandidate = child.Intersect(position, direction, out Hit candidate);

                if (possibleCandidate)
                {
                    var candidatePosition = Vector3d.DistanceSquared(position, candidate.point);
                    if (candidatePosition < closestPosition )
                    {
                        closestPosition = candidatePosition;
                        closest = candidate;
                    }
                }
            }
            if (closest == default)
            {
                t = default;
                return false;
            }
            closest.point = Vector3d.TransformPosition(closest.point, transformMatrix);
            closest.normal = Vector3d.TransformNormalInverse(closest.normal,invertedMatrix);
            t = closest;
            // System.Console.WriteLine(closest.material.color);
            return true;
        }
    }
    /// <summary>
    /// Original scene class, now not used and it should not be used. There is no hierarchy here, only the list of solids. There is also a possibility to use depth sort, although it is not maybe 
    /// the best decision, so I would not recommend using that, because it would become inefficient.
    /// </summary>
    public class Scene : AbstractScene
    {

        public List<Solid> Objects { private set; get; }

        public Scene(float[] background)
        {
            this.Objects = new();
            this.Lights = new();
            this.backgroundColor = background;
        }

        internal void SortSolidsByView(Vector3d viewVector)
        {
            Objects.Sort(new SolidComparer(viewVector));
        }
        public void AddSolid(Solid s)
        {
            Objects.Add(s);
        }


        public void SaveToFile(string fileName)
        {
            string savable = JsonSerializer.Serialize(this);
            File.WriteAllText(fileName, savable);
        }

        public override bool IntersectScene(Vector3d position, Vector3d direction, out Hit t)
        {
            double closest = double.MaxValue;
            Hit candidateHit = default;
            foreach (var x in Objects)
            {
                bool candidate = x.Intersect(position, direction, out Hit possibleHit);
                if (candidate)
                {
                    double candidateDist = Vector3d.DistanceSquared(position, possibleHit.point);
                    if (candidateDist < closest && possibleHit.t > 10e-6)
                    {
                        closest = candidateDist;
                        candidateHit = possibleHit;
                    }
                    /*if (!compareSolid)
                    {
                        return true;
                    }*/
                    //if (solid != t.solid) return true;
                }
            }
            if (closest != double.MaxValue)
            {
                t = candidateHit;
                return true;
            }
            t = default;
            return false;
        }

        class SolidComparer : IComparer<Solid>
        {
            Vector3d viewPosition;
            public SolidComparer(Vector3d viewPos)
            {
                this.viewPosition = viewPos;
            }
            public int Compare(Solid? x, Solid? y)
            {
                var xL = (x!.Position - viewPosition).Length;
                var yL = (y!.Position - viewPosition).Length;

                return xL.CompareTo(yL);
            }
        }
    }
}