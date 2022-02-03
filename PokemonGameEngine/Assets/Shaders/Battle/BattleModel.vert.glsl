#version 330 core

const int MAX_LIGHTS = 4;

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;

out vec2 pass_uv;
out vec3 pass_shadowPosData;
out vec3 pass_posRelativeToCam;
out vec3 pass_normal;
out vec3 pass_vecToLight[MAX_LIGHTS];
out vec3 pass_vecToCamera;

uniform mat4 u_transform;
uniform mat4 u_view;
uniform mat4 u_projection;
uniform mat4 u_shadowTextureConversion;
uniform vec3 u_cameraPos;
uniform uint u_numLights;
uniform vec3 u_lightPos[MAX_LIGHTS];

void main()
{
    vec4 worldPos = u_transform * vec4(in_position, 1);
    vec4 posRelativeToCam = u_view * worldPos;
    gl_Position = u_projection * posRelativeToCam;
    pass_shadowPosData = (u_shadowTextureConversion * worldPos).xyz;
    pass_posRelativeToCam = posRelativeToCam.xyz;

    pass_uv = in_uv;
    pass_normal = (u_transform * vec4(in_normal, 0)).xyz; // Update normal
    
    for (uint i = 0u; i < u_numLights; i++)
    {
        pass_vecToLight[i] = u_lightPos[i] - worldPos.xyz;
    }
    pass_vecToCamera = normalize(u_cameraPos - worldPos.xyz);
}