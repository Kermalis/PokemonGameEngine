#version 330 core

const float PIXELATE_RECTS = 10;
const float PIXELATE_STEPS = 10;

in vec2 pass_uv;

out vec4 out_color;

uniform int u_outputShadow;
uniform sampler2D u_texture;
uniform float u_opacity;
uniform vec3 u_maskColor;
uniform float u_maskColorAmt;
uniform float u_blacknessAmt;
uniform float u_pixelateAmt;


vec2 pixelate()
{
    float steps = ceil(u_pixelateAmt * PIXELATE_STEPS) / PIXELATE_STEPS;
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
    // First modify UVs and then sample
    out_color = texture(u_texture, pixelate());
    if (out_color.a < 1)
    {
        discard; // Discard transparent pixels
    }
    // If we're writing to the shadow textures:
    if (u_outputShadow != 0)
    {
        out_color = vec4(u_opacity, 0, 0, 1); // Store opacity in r of the shadow FBO's color texture
        return;
    }
    // Normal output below
    out_color.a *= u_opacity;
    out_color.rgb = mix(out_color.rgb, u_maskColor, u_maskColorAmt);
    out_color.rgb = mix(out_color.rgb, vec3(0), u_blacknessAmt);
}