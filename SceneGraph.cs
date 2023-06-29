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

    public void Render(Shader shader, Vector3 viewForward, Matrix4 view, Matrix4 projection) {
        foreach (var child in _children) {
            child.Render(shader, viewForward, Matrix4.Identity, view, projection, _lights);
        }
    }
}

public class GraphElement {
    private readonly Mesh _mesh;
    private readonly Matrix4 _objectToParentSpace;
    private readonly Texture _texture;
    private readonly List<GraphElement> _children;
    private readonly Material _material;
    
    public GraphElement(Mesh mesh, Matrix4 objectToParentSpace, Texture texture,
        List<GraphElement> children, Material material) {
        this._mesh = mesh;
        this._objectToParentSpace = objectToParentSpace;
        this._texture = texture;
        this._children = children;
        this._material = material;
    }

    public void Render(Shader shader, Vector3 viewForward, Matrix4 parentToWorld, Matrix4 view, Matrix4 projection, List<Light> lights) {
        Matrix4 objectToWorld = _objectToParentSpace * parentToWorld;
        _mesh.Render(shader, viewForward, objectToWorld, projection, view, _texture, _material, lights);

        foreach (var child in _children) {
            child.Render(
                shader,
                viewForward,
                objectToWorld,
                view,
                projection,
                lights
            );
        }
    }
}