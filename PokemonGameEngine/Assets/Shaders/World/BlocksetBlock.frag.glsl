#version 330 core

const int MAX_ACTIVE_TEXTURES = 16; // If this is changed, you must also to change the value in GLTextureUtils.cs

flat in int pass_tileset;
in vec2 pass_uv;

out vec4 out_color;

uniform sampler2D u_tilesetTextures[MAX_ACTIVE_TEXTURES];


void main()
{
    out_color = texture(u_tilesetTextures[pass_tileset], pass_uv);
}