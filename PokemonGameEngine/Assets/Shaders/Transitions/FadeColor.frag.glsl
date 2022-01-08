#version 330 core

out vec4 out_color;

uniform vec3 color;
uniform float progress;


void main()
{
    out_color = vec4(color, progress);
}