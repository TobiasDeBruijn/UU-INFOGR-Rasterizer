using OpenTK.Mathematics;

namespace Template; 

public class SceneGraph {

    // Shuld be in sync with the fragment shader
    private const int MaxNumLights = 5;
    
    private readonly List<GraphElement> _children;
    private readonly List<Light> _lights;

    public SceneGraph(List<GraphElement> children, List<Light> lights) {
        if (lights.Count > MaxNumLights) {
            throw new ArgumentOutOfRangeException(nameof(lights),
                $"Too many lights provided. Maximum is {MaxNumLights}, provided {lights.Count}");
        }
        
        this._children = children;
        this._lights = lights;
    }

    public void Render(Shader shader, Frustrum frustrum, Vector3 viewForward, Matrix4 view, Matrix4 projection) {
        foreach (var child in _children) {
            // TODO: Frustrum culling: filter out children out of view
            child.Render(shader, frustrum, viewForward, Matrix4.Identity, view, projection, _lights);
        }
    }
}

public class GraphElement : ICloneable {
    private readonly Mesh _mesh;
    private Matrix4 _objectToParentSpace;
    private readonly Texture _texture;
    private readonly List<GraphElement> _children;
    private readonly Material _material;
    private readonly Sphere _boundingSphere;

    public void AddTransform(Matrix4 transform) {
        _objectToParentSpace = _objectToParentSpace * transform;
    }
    
    public GraphElement(Mesh mesh, Matrix4 objectToParentSpace, Texture texture,
        List<GraphElement> children, Material material) {
        this._mesh = mesh;
        this._objectToParentSpace = objectToParentSpace;
        this._texture = texture;
        this._children = children;
        this._material = material;
        _boundingSphere = mesh.GetBoundingSphere();
    }

    public void Render(Shader shader, Frustrum frustrum, Vector3 viewForward, Matrix4 parentToWorld, Matrix4 view, Matrix4 projection, List<Light> lights) {
        Matrix4 objectToWorld = _objectToParentSpace * parentToWorld;

        // if (_boundingSphere.IsOnFrustrum(frustrum, objectToWorld * view)) {
            _mesh.Render(shader, viewForward, objectToWorld, projection, view, _texture, _material, lights);
        // }
        // } else {
            // Console.WriteLine("Skipping rendering mesh due to frustrum culling");
        // }
        
        
        
        foreach (var child in _children) {
            // TODO: Frustrum culling: filter out children out of view
            child.Render(
                shader,
                frustrum,
                viewForward,
                objectToWorld,
                view,
                projection,
                lights
            );
        }
    }

    public object Clone() {
        return new GraphElement(
            _mesh,
            _objectToParentSpace,
            _texture,
            _children,
            _material
        );
    }
}

internal interface IVolume {
    public bool IsOnFrustrum(Frustrum camFrustrum, Matrix4 modelMatrix);
}

public class Sphere : IVolume {
    private readonly Vector3 _center;
    private readonly float _radius;

    public void Print(Frustrum f) {
        Console.WriteLine($"{_center} {_radius}");
        Console.WriteLine($"F {IsOnOrForwardPlane(f.FarFace)} N {IsOnOrForwardPlane(f.NearFace)} T {IsOnOrForwardPlane(f.TopFace)} B {IsOnOrForwardPlane(f.BottomFace)} L {IsOnOrForwardPlane(f.LeftFace)} R {IsOnOrForwardPlane(f.RightFace)}");
    }
    
    public Sphere(Vector3 center, float radius) {
        _center = center;
        _radius = radius;
    }

    public bool IsOnFrustrum(Frustrum camFrustrum, Matrix4 modelMatrix) {

        Vector3 globalScale = new Vector3(
            modelMatrix.Column0.Length,
            modelMatrix.Column1.Length,
            modelMatrix.Column2.Length
        );
        Vector3 globalCenter = (modelMatrix * new Vector4(_center, 1.0f)).Xyz;

        float maxScale = Math.Max(Math.Max(globalScale.X, globalScale.Y), globalScale.Z);
        Sphere globalSphere = new Sphere(globalCenter, _radius * (maxScale * 0.5f));

        bool result = globalSphere.IsOnOrForwardPlane(camFrustrum.LeftFace)
                      && globalSphere.IsOnOrForwardPlane(camFrustrum.RightFace)
                      && globalSphere.IsOnOrForwardPlane(camFrustrum.TopFace)
                      && globalSphere.IsOnOrForwardPlane(camFrustrum.NearFace)
                      && globalSphere.IsOnOrForwardPlane(camFrustrum.FarFace)
                      && globalSphere.IsOnOrForwardPlane(camFrustrum.BottomFace);

        return result;
    }

    private bool IsOnOrForwardPlane(Plane plane) {
        return plane.GetSignedDistanceToPlane(_center) >= -_radius;
    }
}

public readonly struct Plane {
    private readonly Vector3 _normal;
    private readonly float _distance;

    private Plane(Vector3 normal, float distance) {
        _normal = normal;
        _distance = distance;
    }

    public static Plane FromPoint(Vector3 point, Vector3 normal) {
        Vector3 normal1 = Vector3.Normalize(normal);
        float distance = Vector3.Dot(normal1, point);

        return new Plane(normal, distance);
    }
    
    public float GetSignedDistanceToPlane(Vector3 point) {
        return Vector3.Dot(_normal, point) - _distance;
    }
}

public class Camera {
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public Vector3 Position { get; private set; }

    public Camera(Vector3 position, float yaw, float pitch) {
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
    }

    public void SetPosition(Vector3 position) {
        this.Position = position;
    }

    public void SetRotation(float yaw, float pitch) {
        this.Yaw = yaw;
        this.Pitch = pitch;
    }
    
    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + Front, Up);

    public Vector3 Front => Vector3.Normalize(new Vector3(
        (float) (Math.Cos(MathHelper.DegreesToRadians(Yaw)) * Math.Cos(MathHelper.DegreesToRadians(Pitch))),
        (float) Math.Sin(MathHelper.DegreesToRadians(Pitch)),
        (float) (Math.Sin(MathHelper.DegreesToRadians(Yaw)) * Math.Cos(MathHelper.DegreesToRadians(Pitch)))
    ));
    /// <summary>
    /// The camera's right vector
    /// </summary>
    public Vector3 Right => Vector3.Normalize(
        Vector3.Cross(Front, Vector3.UnitY)
    );
    /// <summary>
    /// The camera's up vector
    /// </summary>
    public Vector3 Up => Vector3.Normalize(
        Vector3.Cross(
            Right, 
            Front
        )
    );
}

public class Frustrum {
    public Plane TopFace, BottomFace, RightFace, LeftFace, FarFace, NearFace;

    private Frustrum(Plane topFace, Plane bottomFace, Plane rightFace, Plane leftFace, Plane farFace, Plane nearFace) {
        TopFace = topFace;
        BottomFace = bottomFace;
        RightFace = rightFace;
        LeftFace = leftFace;
        FarFace = farFace;
        NearFace = nearFace;
    }

    public static Frustrum CreateForCamera(Camera cam, float aspect, float fovY, float zNear, float zFar) {
        float halfVSide = zFar * (float) Math.Tan(fovY * 0.5f);
        float halfHSide = halfVSide * aspect;
        Vector3 frontMultFar = zFar * cam.Front;
        
        Plane nearFace = Plane.FromPoint(cam.Position + zNear * cam.Front, cam.Front);
        Plane farFace = Plane.FromPoint(cam.Position + frontMultFar, -cam.Front);
        Plane rightFace = Plane.FromPoint(
            cam.Position, Vector3.Cross(frontMultFar - cam.Right * halfHSide, cam.Up)
        );
        Plane leftFace = Plane.FromPoint(
            cam.Position, Vector3.Cross(cam.Up, frontMultFar + cam.Right * halfHSide)
        );
        Plane topFace = Plane.FromPoint(
            cam.Position, Vector3.Cross(cam.Right, frontMultFar - cam.Up * halfVSide)
        );
        Plane bottomFace = Plane.FromPoint(
            cam.Position, Vector3.Cross(frontMultFar + cam.Up * halfVSide, cam.Right)
        );

        return new Frustrum(
            topFace,
            bottomFace,
            rightFace,
            leftFace,
            farFace,
            nearFace
        );
    }
}