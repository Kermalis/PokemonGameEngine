#version 330 core

layout(location = 0) in ivec2 in_position;

uniform uvec2 screenSize;


vec2 RelToGL(vec2 v)
{
    return vec2(v.x * 2 - 1, v.y * -2 + 1);
}

void main()
{
    vec2 relPos = vec2(in_position) / screenSize; // Abs to Rel
    gl_Position = vec4(RelToGL(relPos), 0, 1);
}