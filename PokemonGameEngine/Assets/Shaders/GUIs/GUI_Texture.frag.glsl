#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D guiTexture;

void main()
{
    out_color = texture(guiTexture, pass_uv);
}