#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_uv;

void main()
{
    gl_Position = vec4(in_position, 0, 1);
    pass_uv = (in_position * 0.5) + 0.5; // GL position to relative position
}