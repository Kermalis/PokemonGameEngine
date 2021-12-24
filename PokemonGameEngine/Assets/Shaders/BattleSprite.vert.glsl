#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_uv;

uniform mat4 transformViewProjection;


void main()
{
    gl_Position = transformViewProjection * vec4(in_position, 0, 1);
    // Convert vertices coords to uvs
    pass_uv.x = in_position.x + 0.5; // -0.5 -> 0 | 0.5 -> 1
    pass_uv.y = 1 - in_position.y; // 0 -> 1 | 1 -> 0
}