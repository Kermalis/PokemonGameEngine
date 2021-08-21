#version 330 core

in vec2 textureCoords;

out vec4 finalColor;

uniform sampler2D guiTexture;

void main()
{
    finalColor = texture(guiTexture, textureCoords);
}