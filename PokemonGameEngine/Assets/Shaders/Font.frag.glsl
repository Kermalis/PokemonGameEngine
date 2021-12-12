#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform usampler2D fontTexture;
uniform uint numFontColors;
uniform vec4 fontColors[256];

void main()
{
    uint colorIdx = texture(fontTexture, pass_uv).r; // We stored the color indices in the r component
    if (colorIdx >= numFontColors)
    {
        discard;
    }
    out_color = fontColors[colorIdx];
}