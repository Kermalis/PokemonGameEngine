#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D imgTexture;
uniform float opacity;
uniform vec3 maskColor;
uniform float maskColorAmt;


void main()
{
    out_color = texture(imgTexture, pass_uv);
    if (out_color.a < 1)
    {
        discard;
    }
    out_color.a *= opacity;
    out_color.rgb = mix(out_color.rgb, maskColor, maskColorAmt);
}