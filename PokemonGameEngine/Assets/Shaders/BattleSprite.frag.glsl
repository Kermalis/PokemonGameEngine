#version 330 core

const float PIXELATE_RECTS = 10;
const float PIXELATE_STEPS = 10;

in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D imgTexture;
uniform float opacity;
uniform vec3 maskColor;
uniform float maskColorAmt;
uniform float pixelateAmt;


vec2 pixelate()
{
    float steps = ceil(pixelateAmt * PIXELATE_STEPS) / PIXELATE_STEPS;
    if (steps <= 0) // Value between 0 and PIXELATE_STEPS inclusive
    {
        return pass_uv; // Happens at progress 0, return unaltered uv
    }
    float rectSize = steps / PIXELATE_RECTS;
    vec2 newUV = floor(pass_uv / rectSize) * rectSize; // Create the rectangles by flooring
    return clamp(newUV, vec2(0, 0), vec2(1, 1)); // Prevent texture wrapping
}

void main()
{
    out_color = texture(imgTexture, pixelate());
    if (out_color.a < 1)
    {
        discard;
    }
    out_color.a *= opacity;
    out_color.rgb = mix(out_color.rgb, maskColor, maskColorAmt);
}