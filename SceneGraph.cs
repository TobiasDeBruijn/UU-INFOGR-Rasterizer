using OpenTK.Mathematics;

namespace Template; 

public class SceneGraph {
    private readonly List<GraphElement> _children;

    public SceneGraph(List<GraphElement> children) {
        this._children = children;
    }

    public void Render(Shader shader, Matrix4 worldToScreen) {
        foreach (var child in _children) {
            child.Render(shader, Matrix4.Identity, worldToScreen);
        }
    }
}

public class GraphElement {
    private readonly Mesh _mesh;
    private readonly Matrix4 _objectToParentSpace;
    private readonly Texture _texture;
    private readonly List<GraphElement> _children;
    private readonly RenderInformation _renderInformation;
    
    public GraphElement(Mesh mesh, RenderInformation renderInformation, Matrix4 objectToParentSpace, Texture texture,
        List<GraphElement> children) {
        this._mesh = mesh;
        this._objectToParentSpace = objectToParentSpace;
        this._texture = texture;
        this._children = children;
        this._renderInformation = renderInformation;
    }

    public void Render(Shader shader, Matrix4 parentToWorld, Matrix4 worldToScreen) {
        Matrix4 objectToScreen =
            _objectToParentSpace
            * parentToWorld
            * worldToScreen;

        Matrix4 objectToWorld = _objectToParentSpace * parentToWorld;
        _mesh.Render(shader, objectToScreen, objectToWorld, _texture, _renderInformation);

        foreach (var child in _children) {
            child.Render(
                shader,
                objectToWorld,
                worldToScreen
            );
        }
    }
}