#version 330 core

in vec2 pass_absPos;
in vec2 pass_uv;

out vec4 out_color;

uniform ivec2 size;
uniform ivec4 cornerRadii; // TopLeft,BottomLeft,TopRight,BottomRight
uniform int lineThickness;
uniform int useTexture;
uniform vec4 lineColor;
uniform vec4 color;
uniform float opacity;
uniform sampler2D guiTexture;


float getDist(vec2 halfSize, float r)
{
    return 1 - (length(max(abs(pass_absPos - halfSize) - halfSize + r, 0)) - r);
}
bool discardPixels()
{
    vec2 halfSize = size * 0.5;
    ivec2 xSide = pass_absPos.x < halfSize.x ? cornerRadii.xy : cornerRadii.zw;
    int cornerRadius = pass_absPos.y < halfSize.y ? xSide.x : xSide.y;

    // Discard pixels for corner radius
    if (cornerRadius != 0 && getDist(halfSize, cornerRadius) < 1)
    {
        discard;
    }
    // Discard pixels for line thickness
    if (lineThickness != 0)
    {    
        if (getDist(halfSize, cornerRadius + lineThickness) < 1 + lineThickness)
        {
            return true; // Returns true if we're drawing the line
        }
        if (color.a == 0)
        {
            discard;
        }
    }
    return false;
}

void main()
{
    if (discardPixels())
    {
        out_color = lineColor;
    }
    else if (useTexture != 0)
    {
        out_color = texture(guiTexture, pass_uv);
    }
    else
    {
        out_color = color;
    }
    out_color.a *= opacity;
}