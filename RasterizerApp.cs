using System.Diagnostics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Template; 

internal class RasterizerApp {
    // member variables
    public readonly Surface Screen;                  // background surface for printing etc.
    private Mesh? _teapot, _floor;                   // meshes to draw using OpenGL
    private float _a;                                // teapot rotation angle
    private readonly Stopwatch _timer = new();       // timer for measuring frame duration
    private Shader? _shader;                         // shader to use for rendering
    private Shader? _postproc;                       // shader to use for post processing
    private Texture? _wood, _metal, _carbon;                          // texture to use for rendering
    private RenderTarget? _target;                   // intermediate render target
    private ScreenQuad? _quad;                       // screen filling quad for post processing
    private const bool UseRenderTarget = false;      // required for post processing

    private readonly Camera _camera = new(new Vector3(3f, 23f, 28f), -94.5f, -24.5f);
    private Frustrum Frustrum => Frustrum.CreateForCamera(_camera, AspectRatio, MathHelper.DegreesToRadians(FieldOfView), DepthNear, DepthFar);

    /// <summary>
    /// Field of view in degrees
    /// </summary>
    private const float FieldOfView = 60;
    
    /// <summary>
    /// Distance to the near clip plane
    /// </summary>
    private const float DepthNear = 0.1f;
    
    /// <summary>
    /// Distance to the far clip plane
    /// </summary>
    private const float DepthFar = 1000f;
   
    /// <summary>
    /// The view matrix.
    /// Transforms from world space to camera space.
    /// </summary>
    /// <summary>
    /// Aspect ration of the view plane
    /// </summary>
    private float AspectRatio => (float) Screen.width / Screen.height;

    private static readonly Vector3 AmbientColor = new Vector3(0.1f);
    private static readonly Vector3 SpecularColor = new Vector3(0.3f);
    
    /// <summary>
    /// The projection matrix.
    /// Transforms from camera space to screen space.
    /// </summary>
    private Matrix4 ProjectionMatrix =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), AspectRatio, DepthNear, DepthFar);
    
    // constructor
    public RasterizerApp(Surface screen) {
        this.Screen = screen;
    }
    // initialize
    public void Init() {
        // Load meshes
        _teapot = new Mesh("../../../assets/teapot.obj");
        _floor = new Mesh("../../../assets/floor.obj");
        
        // initialize stopwatch
        _timer.Reset();
        _timer.Start();
        
        // create shaders
        _shader = new Shader("../../../shaders/vertex.glsl", "../../../shaders/frag.glsl");
        _postproc = new Shader("../../../shaders/vs_post.glsl", "../../../shaders/fs_post.glsl");
        
        // load a texture
        _wood = new Texture("../../../assets/wood.jpg");
        _metal = new Texture("../../../assets/metal.png");
        _carbon = new Texture("../../../assets/carbon.jpeg");
        
        // create the render target
        if (UseRenderTarget) {
            _target = new RenderTarget(Screen.width, Screen.height);
        }
        
        _quad = new ScreenQuad();

        Console.WriteLine("The tea is brewing...A moment of patience please.");
    }

    // tick for background surface
    public void Tick() {
        Screen.Clear(0);
    }

    // tick for OpenGL rendering code
    public void RenderGl() {
        float frameDuration = _timer.ElapsedMilliseconds;
        _timer.Reset();
        _timer.Start();
        
        // update rotation
        _a += 0.001f * frameDuration;
        if (_a > 2 * MathF.PI) {
            _a -= 2 * MathF.PI;
        }
        
        List<Light> lights = new() {
            new Light {
                Position = new Vector3(3, 3, 3),
            },
            new Light {
                Position = new Vector3(-3, -3, -3),
            }
        };

        const int gridSize = 20;
        const int halfGrid = gridSize / 2;

        List<GraphElement> teapots = new(gridSize * gridSize);

        Random random = new Random();
        for (int x = -halfGrid / 2; x < halfGrid; x++) {
            for (int z = -halfGrid; z < halfGrid; z++) {
                if(x == 0 && z == 0) continue;
                
                    teapots.Add(new GraphElement(
                    _teapot!,
                    Matrix4.CreateTranslation(x * 15, 0, z * 15),
                    _metal!,
                    new List<GraphElement>(),
                    new Material() {
                        Ambient = AmbientColor,
                        Specular = SpecularColor,
                        DiffuseTint = new Vector3(
                            random.NextSingle(),
                            random.NextSingle(),
                            random.NextSingle()
                        ),
                        Specularity = 30f,
                    }
                ));
            }   
        }

        SceneGraph sceneGraph = new SceneGraph(new List<GraphElement>()
            {
                new(
                    _teapot!,
                    Matrix4.CreateRotationY(_a) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-15)),
                    _metal!,
                    new List<GraphElement>() {
                        new(
                            _teapot!,
                            Matrix4.CreateTranslation(0, 10, 0)
                            * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90)),
                            _wood!,
                            new List<GraphElement>(),
                            new Material {
                                Ambient = AmbientColor,
                                DiffuseTint = Vector3.One,
                                Specular = SpecularColor,
                                Specularity = 30f,
                            }
                        ),
                    },
                    new Material{
                        Ambient = AmbientColor,
                        DiffuseTint = Vector3.One,
                        Specular = SpecularColor,
                        Specularity = 20f,
                    }
                ),
                new(
                    _floor!,
                    Matrix4.CreateFromAxisAngle(Vector3.UnitY, -_a)
                    * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(10)),
                    _carbon!,
                    teapots,
                    new Material{
                        Ambient = AmbientColor,
                        DiffuseTint = Vector3.UnitY,
                    }

                )
            },
            lights
        );
        if (UseRenderTarget && _target != null && _quad != null) {
            // enable render target
            _target.Bind();
            
            // render scene to render target
            if (_shader != null && _wood != null) {
                sceneGraph.Render(_shader, Frustrum, _camera.Front, _camera.ViewMatrix, ProjectionMatrix);
            }
            
            // render quad
            _target.Unbind();
            if (_postproc != null) {
                _quad.Render(_postproc, _target.GetTextureID());
            }
        }  else {
            // render scene directly to the screen
            if (_shader != null && _wood != null) {
                sceneGraph.Render(_shader, Frustrum, _camera.Front, _camera.ViewMatrix, ProjectionMatrix);
            }
        }
    }
    
    /// <summary>
    /// Key press handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnKeyPress(KeyboardKeyEventArgs e) {
        Vector3 moveScaler = new Vector3(1f);
        Vector3 position = _camera.Position;
        Vector3 newPosition = e.Key switch {
            Keys.W => position + _camera.Front * moveScaler,
            Keys.A => position - _camera.Right * moveScaler,
            Keys.S => position - _camera.Front * moveScaler,
            Keys.D => position + _camera.Right * moveScaler,
            Keys.Space => position + Vector3.UnitY * moveScaler,
            Keys.LeftShift or Keys.RightShift or Keys.Z => position - Vector3.UnitY * moveScaler,
            _ => position
        };
        
        _camera.SetPosition(newPosition);
    }
    
    /// <summary>
    /// Mouse movement handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnMouseMove(MouseMoveEventArgs e) {
        const float sensitivity = 4;

        float yaw = _camera.Yaw + e.DeltaX / 360 * sensitivity;
        float pitch = _camera.Pitch - e.DeltaY / 360 * sensitivity;
        _camera.SetRotation(yaw, pitch);
    }
}