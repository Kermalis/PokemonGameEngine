#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D u_texture;

void main()
{
    out_color = texture(u_texture, pass_uv);
}