#version 330 core

in vec2 pass_absPos;
in vec2 pass_uv;

out vec4 out_color;

uniform ivec2 size;
uniform int cornerRadius;
uniform int lineThickness;
uniform int useTexture;
uniform vec4 color;
uniform sampler2D guiTexture;


float getDist(vec2 halfSize, float r)
{
    return 1 - (length(max(abs(pass_absPos - halfSize) - halfSize + r, 0)) - r);
}
void discardPixels()
{
    // Discard pixels for corner radius
    vec2 halfSize = size * 0.5;
    if (getDist(halfSize, cornerRadius) < 1)
    {
        discard;
    }
    // Discard pixels for line thickness
    if (lineThickness != 0)
    {
        if (getDist(halfSize, cornerRadius + lineThickness) >= 1 + lineThickness)
        {
            discard;
        }
    }
}

void main()
{
    discardPixels();
    if (useTexture != 0)
    {
        out_color = texture(guiTexture, pass_uv);
    }
    else
    {
        out_color = color;
    }
}