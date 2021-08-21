#version 330 core

in vec2 TexCoords;

out vec4 color;

uniform sampler2D texture_diffuse1;

void main()
{
    vec4 texel = texture(texture_diffuse1, TexCoords);
    if (texel.a < 0.5)
    {
        discard;
    }
    color = texel;
}