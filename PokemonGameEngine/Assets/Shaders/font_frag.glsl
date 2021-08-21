#version 330 core

in vec2 textureCoords;

out vec4 finalColor;

uniform usampler2D fontTexture;
uniform vec4 fontColors[256];
uniform uint numFontColors;

void main()
{
    uint colorIdx = texture(fontTexture, textureCoords).r; // We stored the color indices in the r component
    if (colorIdx >= numFontColors)
    {
        discard;
    }
    finalColor = fontColors[colorIdx];
}