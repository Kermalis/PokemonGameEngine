#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_uv;

uniform mat4 projection;
uniform mat4 transformView; // View matrix and sprite's transform combined to always face the camera


void main()
{
    gl_Position = projection * transformView * vec4(in_position, 0, 1);
    // Convert vertices coords to uvs
    pass_uv.x = in_position.x + 0.5; // -0.5 -> 0 | 0.5 -> 1
    pass_uv.y = 1 - in_position.y; // 0 -> 1 | 1 -> 0
}