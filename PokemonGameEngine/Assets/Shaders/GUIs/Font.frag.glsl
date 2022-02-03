#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform usampler2D u_texture;
uniform uint u_numColors;
uniform vec4 u_colors[256];

void main()
{
    uint colorIdx = texture(u_texture, pass_uv).r; // We stored the color indices in the r component
    if (colorIdx >= u_numColors)
    {
        discard;
    }
    out_color = u_colors[colorIdx];
}