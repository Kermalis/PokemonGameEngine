#version 330 core

const int MAX_LIGHTS = 4;

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec2 texCoords;

out vec2 TexCoords;
out vec3 Normal;
out vec3 VecToLight[MAX_LIGHTS];
out vec3 VecToCamera;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 cameraPos;
uniform uint numLights;
uniform vec3 lightPos[MAX_LIGHTS];

void main()
{
    vec4 worldPos = model * vec4(position, 1.0);
    gl_Position = projection * view * worldPos;

    TexCoords = texCoords;

    Normal = (model * vec4(normal, 0.0)).xyz;
    for (uint i = 0u; i < numLights; i++)
    {
        VecToLight[i] = lightPos[i] - worldPos.xyz;
    }
    VecToCamera = cameraPos - worldPos.xyz;
}