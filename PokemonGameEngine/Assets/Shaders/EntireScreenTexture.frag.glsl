#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D uTexture;

void main()
{
    out_color = texture(uTexture, pass_uv);
}