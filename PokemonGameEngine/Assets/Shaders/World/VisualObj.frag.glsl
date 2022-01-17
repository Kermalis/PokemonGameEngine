#version 330 core

in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D objTexture;


void main()
{
    out_color = texture(objTexture, pass_uv);
    if (out_color.a < 0.5)
    {
        discard;
    }
}