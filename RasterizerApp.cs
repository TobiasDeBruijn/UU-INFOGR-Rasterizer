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
    private Texture? _wood;                          // texture to use for rendering
    private RenderTarget? _target;                   // intermediate render target
    private ScreenQuad? _quad;                       // screen filling quad for post processing
    private const bool UseRenderTarget = false;       // required for post processing

    private Vector3 _cameraPosition = Vector3.Zero + new Vector3(0, -14.5f, 0);
    private float _cameraYaw;
    private float _cameraPitch;
    private const float FieldOfView = 60;
    private const float DepthNear = 0.1f;
    private const float DepthFar = 1000f;
    
    private Vector3 CameraForwardDirection => new((float)(Math.Cos(_cameraPitch) * Math.Sin(_cameraYaw)),
        (float)-Math.Sin(_cameraPitch), (float)(Math.Cos(_cameraPitch) * Math.Cos(_cameraYaw)));
    
    private Vector3 CameraRightDirection =>
        new((float)Math.Cos(_cameraYaw), 0, (float)-Math.Sin(_cameraYaw));
    
    private Vector3 CameraUpDirection =>
        Vector3.Cross(CameraRightDirection, CameraForwardDirection);
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

        LightSource[] lightSources = new[] {
            new LightSource(Vector3.UnitX, new Vector3(10f, 10f, 10f), 1f),
        };
        
        SceneGraph graph = new SceneGraph(
            new List<GraphElement>() {
                new(
                    _teapot!,
                    new RenderInformation() {
                        LightSources = lightSources,
                        Material = new Material {
                            AmbientLightColor = Vector3.UnitZ,
                            AmbientLightEmittance = 0.5f,
                            DiffuseColor = Vector3.UnitY,
                        }, 
                    },
                Matrix4.CreateScale(0.5f),
                    // * Matrix4.CreateFromAxisAngle(Vector3.UnitZ, _a),
                    _wood!, 
                    new List<GraphElement>() { 
                        new(
                            _teapot!, 
                            new RenderInformation() {
                                LightSources = lightSources,
                                Material = new Material {
                                    AmbientLightColor = Vector3.One,
                                    AmbientLightEmittance = 0.5f,
                                },
                            },
                            Matrix4.CreateTranslation(3, 3, 3)
                            * Matrix4.CreateScale(0.8f),
                            _wood!, 
                            new List<GraphElement>()
                        )
                    }
                ),
                new(
                    _floor!,
                    new RenderInformation() {
                        LightSources = lightSources,
                        Material = new Material {
                            AmbientLightColor = new Vector3(1.0f, 0.0f, 0.0f),
                            AmbientLightEmittance = 0.5f,
                        }, 
                    },
                    Matrix4.CreateFromAxisAngle(
                        Vector3.UnitX,
                        MathHelper.DegreesToRadians(0)
                    ), 
                    _wood!, 
                    new List<GraphElement>()
                )
            }
        );
        
        // Apply camera position and rotation
        Matrix4 worldToCamera =
            Matrix4.CreateTranslation(_cameraPosition)
            * Matrix4.CreateFromAxisAngle(Vector3.UnitX, _cameraPitch)
            * Matrix4.CreateFromAxisAngle(Vector3.UnitY, _cameraYaw);
        
        // Apply screen perspective and depth clipping.
        Matrix4 cameraToScreen = 
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(FieldOfView), 
                (float) Screen.width / Screen.height, 
                DepthNear, 
                DepthFar
            );
        
        // Combine the two to convert from world space to the screen
        Matrix4 worldToScreen = worldToCamera * cameraToScreen;
        
        if (UseRenderTarget && _target != null && _quad != null) {
            // enable render target
            _target.Bind();

            // render scene to render target
            if (_shader != null && _wood != null) {
                graph.Render(_shader, worldToScreen);
            }

            // render quad
            _target.Unbind();
            if (_postproc != null) {
                _quad.Render(_postproc, _target.GetTextureID());
            }
        }  else {
            // render scene directly to the screen
            if (_shader != null && _wood != null) {
                graph.Render(_shader, worldToScreen);
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
            Keys.Space => _cameraPosition - CameraUpDirection * moveScaler,
            Keys.LeftShift or Keys.RightShift => _cameraPosition + CameraUpDirection * moveScaler,
            _ => _cameraPosition
        };
    }
    
    /// <summary>
    /// Mouse movement handler
    /// </summary>
    /// <param name="e">The event arguments</param>
    public void OnMouseMove(MouseMoveEventArgs e) {
        _cameraYaw += e.DeltaX / 360;
        _cameraPitch += e.DeltaY / 360;
    }
}