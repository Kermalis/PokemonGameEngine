#version 330 core

layout(location = 0) in ivec2 in_position;
layout(location = 1) in vec2 in_uv;

out vec2 pass_uv;

uniform ivec2 viewportSize;


vec2 RelToGL(vec2 v)
{
    return vec2(v.x * 2 - 1, v.y * -2 + 1);
}

void main()
{
    vec2 relPos = vec2(in_position) / viewportSize; // Abs to Rel
    gl_Position = vec4(RelToGL(relPos), 0, 1);
    pass_uv = in_uv;
}