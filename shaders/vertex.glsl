#version 440 core
 
in vec3 position;
in vec2 texCoords;
in vec3 normal;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

out vec3 FragPos;
out vec3 Color;
out vec3 Normal;
out vec2 TexCoords;

void main() {
	const vec3 FragPos = vec3(model * vec4(position, 1.0));
	
	gl_Position = projection * view * vec4(FragPos, 1.0);
	
	Color = normal;
	Normal = mat3(model) * normal;
	TexCoords = texCoords;
}

// shader inputs
//in vec3 vertexPositionObject;	// vertex position in Object Space
//in vec3 vertexNormalObject;		// vertex normal in Object Space
//in vec2 vertexUV;				// vertex uv texture coordinates
//uniform mat4 objectToScreen;
//uniform mat4 objectToWorld;
//
//// shader outputs, will be interpolated from vertices to fragments
//out vec4 positionWorld;			// vertex position in World Space
//out vec4 normalWorld;			// vertex normal in World Space
//out vec2 uv;					// vertex uv texture coordinates (pass-through)
// 
//// vertex shader
//void main()
//{
//	// transform vertex position to 2D Screen Space + depth
//	gl_Position = objectToScreen * vec4(vertexPositionObject, 1.0);
//
//	// transform vertex position and normal to an appropriate space for shading calculations
//	positionWorld = objectToWorld * vec4(vertexPositionObject, 1.0);
//	normalWorld = inverse(transpose(objectToWorld)) * vec4(vertexNormalObject, 0.0f);
//	// pass the uv coordinate
//	uv = vertexUV;
//}