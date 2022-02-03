#version 330 core

flat in int pass_color;

out vec4 out_color;

uniform vec3 u_colors[3];


void main()
{
    out_color = vec4(u_colors[pass_color], 1);
}