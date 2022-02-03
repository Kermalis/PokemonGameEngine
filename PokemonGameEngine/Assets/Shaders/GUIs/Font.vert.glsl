#version 330 core

layout(location = 0) in vec2 in_position;
layout(location = 1) in ivec2 in_instancedPos;
layout(location = 2) in ivec2 in_instancedSize;
layout(location = 3) in vec2 in_instancedUVStart;
layout(location = 4) in vec2 in_instancedUVEnd;

out vec2 pass_uv;

uniform ivec2 u_translation;
uniform ivec2 u_viewportSize;


void main()
{
    vec2 relPos = (in_instancedPos + u_translation + (in_position * in_instancedSize)) / u_viewportSize;
    float glX = relPos.x * 2 - 1; // (0 => 1) to (-1 => 1)
    float glY = relPos.y * -2 + 1; // (0 => 1) to (1 => -1)
    gl_Position = vec4(glX, glY, 0, 1);

    pass_uv = mix(in_instancedUVStart, in_instancedUVEnd, in_position);
}