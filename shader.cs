using OpenTK.Graphics.OpenGL;

namespace Template; 

public class Shader {
    // data members
    public readonly int ProgramId;
    public readonly int VertexPositionObjectHandle;
    public readonly int VertexNormalObjectHandle;
    public readonly int VertexUvHandle;
    public readonly int ObjectToScreenHandle;
    public readonly int ObjectToWorldHandle;

    // constructor
    public Shader(string vertexShader, string fragmentShader) {
        ProgramId = GL.CreateProgram();
        GL.ObjectLabel(ObjectLabelIdentifier.Program, ProgramId, -1, vertexShader + " + " + fragmentShader);
            
        Load(vertexShader, ShaderType.VertexShader, ProgramId, out int _);
        Load(fragmentShader, ShaderType.FragmentShader, ProgramId, out int _);
        GL.LinkProgram(ProgramId);
            
        string infoLog = GL.GetProgramInfoLog(ProgramId);
        if (infoLog.Length != 0) {
            // Console.WriteLine(infoLog);
        }

        // get locations of shader parameters
        VertexPositionObjectHandle = GL.GetAttribLocation(ProgramId, "vertexPositionObject");
        VertexNormalObjectHandle = GL.GetAttribLocation(ProgramId, "vertexNormalObject");
        VertexUvHandle = GL.GetAttribLocation(ProgramId, "vertexUV");
        ObjectToScreenHandle = GL.GetUniformLocation(ProgramId, "objectToScreen");
        ObjectToWorldHandle = GL.GetUniformLocation(ProgramId, "objectToWorld");
    }

    // loading shaders
    private static void Load(string filename, ShaderType type, int program, out int id) {
        id = GL.CreateShader(type);
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