#version 330 core

layout(location = 0) in vec2 in_position;
layout(location = 1) in int in_color;

flat out int pass_color;


void main()
{
    gl_Position = vec4(in_position, 0, 1);
    pass_color = in_color;
}