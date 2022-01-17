#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_absPos;
out vec2 pass_uv;

uniform ivec2 pos;
uniform ivec2 size;
uniform int useTexture;
uniform vec2 uvStart;
uniform vec2 uvEnd;
uniform ivec2 viewportSize;


void main()
{
    // First calculate vertex position
    pass_absPos = in_position * size;
    vec2 relPos = (pos + pass_absPos) / viewportSize;
    float glX = relPos.x * 2 - 1; // (0 => 1) to (-1 => 1)
    float glY = relPos.y * -2 + 1; // (0 => 1) to (1 => -1)
    gl_Position = vec4(glX, glY, 0, 1);

    if (useTexture != 0)
    {
        pass_uv = mix(uvStart, uvEnd, in_position);
    }
}