#version 330 core

out vec4 out_color;

uniform vec3 u_color;
uniform float u_progress;


void main()
{
    out_color = vec4(u_color, u_progress);
}