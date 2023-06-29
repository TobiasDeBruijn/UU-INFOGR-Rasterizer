using OpenTK.Graphics.OpenGL;

namespace Template; 

public class Shader {
    public readonly int ProgramId;
    
    // Vertex shader
    public readonly int VertexPositionObjectHandle;
    public readonly int VertexNormalObjectHandle;
    public readonly int VertexUvHandle;
    public readonly int ProjectionMatrixHandle;
    public readonly int ViewMatrixHandle;
    public readonly int ModelMatrixHandle;
    
    // Fragment shader
    public readonly int TextureDiffuse1Handle;
    public readonly int TextureSpecular1Handle;
    public readonly int ViewPositionHandle;
    public readonly int ViewForwardHandle;

    public int LightPositionHandle(int index) => GL.GetUniformLocation(ProgramId, $"lights[{index}].position");
    public readonly int LightCountHandle;
    
    public readonly int MaterialAmbientHandle;
    public readonly int MaterialDiffuseHandle;
    public readonly int MaterialSpecularHandle;
    
    // constructor
    public Shader(string vertexShader, string fragmentShader) {
        ProgramId = GL.CreateProgram();
        GL.ObjectLabel(ObjectLabelIdentifier.Program, ProgramId, -1, vertexShader + " + " + fragmentShader);
            
        Load(vertexShader, ShaderType.VertexShader, ProgramId);
        Load(fragmentShader, ShaderType.FragmentShader, ProgramId);
        
        GL.LinkProgram(ProgramId);
            
        string infoLog = GL.GetProgramInfoLog(ProgramId);
        if (infoLog.Length != 0) {
            Console.WriteLine(infoLog);
        }

        // Vertex
        VertexPositionObjectHandle = GL.GetAttribLocation(ProgramId, "position");
        VertexNormalObjectHandle = GL.GetAttribLocation(ProgramId, "normal");
        VertexUvHandle = GL.GetAttribLocation(ProgramId, "texCoords");
        ProjectionMatrixHandle = GL.GetUniformLocation(ProgramId, "projection");
        ViewMatrixHandle = GL.GetUniformLocation(ProgramId, "view");
        ModelMatrixHandle = GL.GetUniformLocation(ProgramId, "model");
        
        // Fragment
        TextureDiffuse1Handle = GL.GetUniformLocation(ProgramId, "texture_diffuse1");
        // TextureSpecular1Handle = GL.GetUniformLocation(ProgramId, "texture_specular1");
        ViewPositionHandle = GL.GetUniformLocation(ProgramId, "viewPos");
        ViewForwardHandle = GL.GetUniformLocation(ProgramId, "viewForward");

        LightCountHandle = GL.GetUniformLocation(ProgramId, "lightCount");
        
        MaterialAmbientHandle = GL.GetUniformLocation(ProgramId, "material.ambient");
        MaterialDiffuseHandle = GL.GetUniformLocation(ProgramId, "material.diffuse");
        MaterialSpecularHandle = GL.GetUniformLocation(ProgramId, "material.specular");
    }

    // loading shaders
    private static void Load(string filename, ShaderType type, int program) {
        int id = GL.CreateShader(type);
        GL.ObjectLabel(ObjectLabelIdentifier.Shader, id, -1, filename);
        
        using (StreamReader sr = new StreamReader(filename)) {
            GL.ShaderSource(id, sr.ReadToEnd());
        }
            
        GL.CompileShader(id);
        GL.AttachShader(program, id);
            
        string infoLog = GL.GetShaderInfoLog(id);
        if (infoLog.Length != 0) {
            // Console.WriteLine(infoLog);
        }
    }
}