#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform vec3 modification;
uniform sampler2D colorTexture;


void main()
{
    out_color = texture(colorTexture, pass_uv);
    out_color.r *= modification.r;
    out_color.g *= modification.g;
    out_color.b *= modification.b;
}