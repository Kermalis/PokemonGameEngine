#version 330 core

flat in int pass_color;

out vec4 out_color;

uniform vec3 colors[3];

void main()
{
    out_color = vec4(colors[pass_color], 1);
}