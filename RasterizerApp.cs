using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp;

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
    
    /// <summary>
    /// Position of the camera
    /// </summary>
    private Vector3 _cameraPosition = new(0, 9f, 30f);
    /// <summary>
    /// Camera yaw in degrees.
    /// </summary>
    private float _cameraYaw = -90.0f;
    /// <summary>
    /// Camera pitch in degrees
    /// </summary>
    private float _cameraPitch;
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
    /// The camera's forward looking vector.
    /// </summary>
    private Vector3 CameraForwardDirection => Vector3.Normalize(new Vector3(
        (float) (Math.Cos(MathHelper.DegreesToRadians(_cameraYaw)) * Math.Cos(MathHelper.DegreesToRadians(_cameraPitch))),
        (float) Math.Sin(MathHelper.DegreesToRadians(_cameraPitch)),
        (float) (Math.Sin(MathHelper.DegreesToRadians(_cameraYaw)) * Math.Cos(MathHelper.DegreesToRadians(_cameraPitch)))
    ));
    /// <summary>
    /// The camera's right vector
    /// </summary>
    private Vector3 CameraRightDirection => Vector3.Normalize(
        Vector3.Cross(CameraForwardDirection, Vector3.UnitY)
    );
    /// <summary>
    /// The camera's up vector
    /// </summary>
    private Vector3 CameraUpDirection => Vector3.Normalize(
        Vector3.Cross(
            CameraRightDirection, 
            CameraForwardDirection
        )
    );
    /// <summary>
    /// The view matrix.
    /// Transforms from world space to camera space.
    /// </summary>
    private Matrix4 ViewMatrix => Matrix4.LookAt(_cameraPosition, _cameraPosition + CameraForwardDirection, CameraUpDirection);
    /// <summary>
    /// Aspect ration of the view plane
    /// </summary>
    private float AspectRatio => (float) Screen.width / Screen.height;
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

        Vector3 ambientColor = new Vector3(0.1f);
        Vector3 specularColor = new Vector3(0.3f);
        
        SceneGraph graph = new SceneGraph(new List<GraphElement>()
            {
                new(
                    _teapot!,
                    Matrix4.CreateFromAxisAngle(Vector3.UnitY, _a)
                        * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-15)),
                    _metal!,
                    new List<GraphElement>() {
                        new(
                            _teapot!,
                            Matrix4.CreateTranslation(0, 10, 0)
                            * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90)),
                            _wood!,
                            new List<GraphElement>(),
                            new Material {
                                Ambient = ambientColor,
                                Diffuse = Vector3.UnitY,
                                Specular = specularColor,
                            }
                        ),
                    },
                    new Material{
                        Ambient = ambientColor,
                        Diffuse = Vector3.UnitX,
                        Specular = specularColor,
                    }
                ),
                new(
                    _floor!,
                    Matrix4.CreateFromAxisAngle(Vector3.UnitY, -_a)
                        * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(10)),
                    _carbon!,
                    new List<GraphElement>(),
                    new Material{
                        Ambient = ambientColor,
                        Diffuse = Vector3.UnitY,
                        Specular = specularColor,
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
                graph.Render(_shader, CameraForwardDirection, ViewMatrix, ProjectionMatrix);
            }
            
            // render quad
            _target.Unbind();
            if (_postproc != null) {
                _quad.Render(_postproc, _target.GetTextureID());
            }
        }  else {
            // render scene directly to the screen
            if (_shader != null && _wood != null) {
                graph.Render(_shader, CameraForwardDirection, ViewMatrix, ProjectionMatrix);
            }
        }
    }
    
    /// <summary>
    /// Key press handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnKeyPress(KeyboardKeyEventArgs e) {
        Vector3 moveScaler = new Vector3(1f);
        _cameraPosition = e.Key switch {
            Keys.W => _cameraPosition + CameraForwardDirection * moveScaler,
            Keys.A => _cameraPosition - CameraRightDirection * moveScaler,
            Keys.S => _cameraPosition - CameraForwardDirection * moveScaler,
            Keys.D => _cameraPosition + CameraRightDirection * moveScaler,
            Keys.Space => _cameraPosition + Vector3.UnitY * moveScaler,
            Keys.LeftShift or Keys.RightShift => _cameraPosition - Vector3.UnitY * moveScaler,
            _ => _cameraPosition
        };
    }
    
    /// <summary>
    /// Mouse movement handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnMouseMove(MouseMoveEventArgs e) {
        const float sensitivity = 4;

        _cameraYaw += e.DeltaX / 360 * sensitivity;
        _cameraPitch -= e.DeltaY / 360 * sensitivity;
    }
}