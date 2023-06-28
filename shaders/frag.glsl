#version 330

struct Material {
    vec3 ambientLightColor;
    float ambientLightEmittance;
    vec3 diffuseColor;
};

struct LightSource {
    vec3 lightColor;
    vec3 lightPosition;
    float lightEmittance; 
};

// shader inputs
in vec4 positionWorld;              // fragment position in World Space
in vec4 normalWorld;                // fragment normal in World Space
in vec2 uv;                         // fragment uv texture coordinates
uniform sampler2D diffuseTexture;	// texture sampler

uniform Material material;

uniform LightSource[4] lightSources;
uniform int lightSourceCount;

// shader output
out vec4 outputColor;

vec3 computePhong() {
    vec3 l = vec3(5, -3, 0) - positionWorld.xyz;
    float attenuation = 1.0 / dot(l, l);
    float ndotl = max(0.0, dot(normalize(normalWorld.xyz), normalize(l)));
    vec3 diffuseColor = texture(diffuseTexture, uv).rgb;
    return vec3(1, 0, 0) * diffuseColor * attenuation * ndotl;
}

// fragment shader
void main() {
//    vec3 accum = vec3(0, 0, 0);
//    for(int i = 0; i < lightSourceCount; i++) {
//        LightSource l = lightSources[i];
//        accum += PhongForLightSource(l);
//    }
//    
//    vec3 light = computePhong();
//    light += material.ambientLightColor * material.ambientLightEmittance;
//    
//    outputColor = vec4(
//        light,
//        1.0
//    );

    vec3 lightPosition = vec3(3, -4, 0);
    vec3 lightColor = vec3(0, 1, 0);
    
    vec3 L = lightPosition - positionWorld.xyz;
    // vector from surface to light, unnormalized!
    float attenuation = 1.0 / dot(L, L);
    // distance attenuation
    float NdotL = max(0, dot(normalize(normalWorld.xyz), normalize(L)));
    // incoming angle attenuation
    vec3 diffuseColor = texture(diffuseTexture, uv).rgb;
    // texture lookup
    outputColor = vec4(lightColor * diffuseColor * attenuation * NdotL, 1.0);
    outputColor += vec4(vec3(1, 1, 1) * 0.5, 1);
}