using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Template;

public struct Material {
    public Vector3 Ambient;
    public Vector3 Diffuse;
    public Vector3 Specular;
}

public struct Light {
    public Vector3 Position;
}

public class Mesh {
    // data members
    private readonly string _filename; // for improved error reporting
    public ObjVertex[]? Vertices; // vertices (positions and normals in Object Space, and texture coordinates)
    public ObjTriangle[]? Triangles; // triangles (3 indices into the vertices array)
    private int _vertexBufferId; // vertex buffer object (VBO) for vertex data
    private int _triangleBufferId; // element buffer object (EBO) for triangle vertex indices

    // constructor
    public Mesh(string filename) {
        _filename = filename;
        MeshLoader loader = new();
        loader.Load(this, filename);
    }

    // initialization; called during first render
    private void Prepare() {
        if (_vertexBufferId != 0) return;
        // generate interleaved vertex data array (uv/normal/position per vertex)
        GL.GenBuffers(1, out _vertexBufferId);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _vertexBufferId, 8 + _filename.Length, "VBO for " + _filename);
        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices?.Length * Marshal.SizeOf(typeof(ObjVertex))),
            Vertices, BufferUsageHint.StaticDraw);

        // generate triangle index array
        GL.GenBuffers(1, out _triangleBufferId);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _triangleBufferId);
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, _triangleBufferId, 17 + _filename.Length,
            "triangle EBO for " + _filename);
        GL.BufferData(BufferTarget.ElementArrayBuffer,
            (IntPtr)(Triangles?.Length * Marshal.SizeOf(typeof(ObjTriangle))), Triangles, BufferUsageHint.StaticDraw);
    }

    // render the mesh using the supplied shader and matrix
    public void Render(Shader shader, Vector3 viewForward, Matrix4 model, Matrix4 projection, Matrix4 view, Texture diffuseTexture, Material material, List<Light> lights) {
        // on first run, prepare buffers
        Prepare();

        // enable shader
        GL.UseProgram(shader.ProgramId);

        // Set vertex shader variables
        GL.UniformMatrix4(shader.ProjectionMatrixHandle, false, ref projection);
        GL.UniformMatrix4(shader.ViewMatrixHandle, false, ref view);
        GL.UniformMatrix4(shader.ModelMatrixHandle, false, ref model);
        
        GL.EnableVertexAttribArray(shader.VertexPositionObjectHandle);
        GL.EnableVertexAttribArray(shader.VertexNormalObjectHandle);
        GL.EnableVertexAttribArray(shader.VertexUvHandle);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
        
        GL.VertexAttribPointer(shader.VertexUvHandle, 2, VertexAttribPointerType.Float, false, 32, 0);
        GL.VertexAttribPointer(shader.VertexNormalObjectHandle, 3, VertexAttribPointerType.Float, true, 32, 2 * 4);
        GL.VertexAttribPointer(shader.VertexPositionObjectHandle, 3, VertexAttribPointerType.Float, false, 32, 5 * 4);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _triangleBufferId);
        
        // Set fragment shader variables
        GL.Uniform1(shader.TextureDiffuse1Handle, 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, diffuseTexture.id);
        
        // GL.Uniform1(shader.TextureSpecular1Handle, 1);
        // GL.ActiveTexture(TextureUnit.Texture1);
        // GL.BindTexture(TextureTarget.Texture2D, diffuseTexture.id);

        for (int idx = 0; idx < lights.Count; idx++) {
            GL.Uniform3(shader.LightPositionHandle(idx), lights[idx].Position);
        }
        
        GL.Uniform1(shader.LightCountHandle, lights.Count);
        
        GL.Uniform3(shader.MaterialDiffuseHandle, material.Diffuse);
        GL.Uniform3(shader.MaterialAmbientHandle, material.Ambient);
        GL.Uniform3(shader.MaterialSpecularHandle, material.Specular);
        
        GL.Uniform3(shader.ViewForwardHandle, viewForward);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, Triangles!.Length * 3);
        
        // restore previous OpenGL state
        GL.UseProgram(0);
    }

    // layout of a single vertex
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjVertex {
        public Vector2 TexCoord;
        public Vector3 Normal;
        public Vector3 Vertex;
    }

    // layout of a single triangle
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjTriangle {
        public int Index0, Index1, Index2;
    }
}