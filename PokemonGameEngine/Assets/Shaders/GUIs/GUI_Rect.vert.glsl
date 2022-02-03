#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_absPos;
out vec2 pass_uv;

uniform ivec2 u_pos;
uniform ivec2 u_size;
uniform int u_useTexture;
uniform vec2 u_uvStart;
uniform vec2 u_uvEnd;
uniform ivec2 u_viewportSize;


void main()
{
    // First calculate vertex position
    pass_absPos = in_position * u_size;
    vec2 relPos = (u_pos + pass_absPos) / u_viewportSize;
    float glX = relPos.x * 2 - 1; // (0 => 1) to (-1 => 1)
    float glY = relPos.y * -2 + 1; // (0 => 1) to (1 => -1)
    gl_Position = vec4(glX, glY, 0, 1);

    if (u_useTexture != 0)
    {
        pass_uv = mix(u_uvStart, u_uvEnd, in_position);
    }
}