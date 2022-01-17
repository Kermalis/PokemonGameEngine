#version 330 core

const int MAX_LIGHTS = 4;

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec3 in_normal;
layout(location = 2) in vec2 in_uv;

out vec2 pass_uv;
out vec3 pass_shadowPosData;
out vec3 pass_posRelativeToCam;
out vec3 pass_normal;
out vec3 pass_vecToLight[MAX_LIGHTS];
out vec3 pass_vecToCamera;

uniform mat4 transform;
uniform mat4 view;
uniform mat4 projection;
uniform mat4 shadowTextureConversion;
uniform vec3 cameraPos;
uniform uint numLights;
uniform vec3 lightPos[MAX_LIGHTS];

void main()
{
    vec4 worldPos = transform * vec4(in_position, 1);
    vec4 posRelativeToCam = view * worldPos;
    gl_Position = projection * posRelativeToCam;
    pass_shadowPosData = (shadowTextureConversion * worldPos).xyz;
    pass_posRelativeToCam = posRelativeToCam.xyz;

    pass_uv = in_uv;
    pass_normal = normalize((transform * vec4(in_normal, 0)).xyz); // Update normal
    
    for (uint i = 0u; i < numLights; i++)
    {
        pass_vecToLight[i] = lightPos[i] - worldPos.xyz;
    }
    pass_vecToCamera = normalize(cameraPos - worldPos.xyz);
}