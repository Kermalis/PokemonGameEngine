#version 330 core

in vec3 pass_uvw;

out vec4 out_color;

uniform sampler3D u_blocksetTexture;


void main()
{
    out_color = texture(u_blocksetTexture, pass_uvw);
}