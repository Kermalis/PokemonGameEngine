#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_uv;

uniform mat4 transformViewProjection;


void main()
{
    float glX = in_position.x - 0.5; // (0 => 1) to (-0.5 => 0.5) (center align)
    float glY = 1 - in_position.y; // (0 => 1) to (1 => 0) (bottom align)
    gl_Position = transformViewProjection * vec4(glX, glY, 0, 1);

    pass_uv = in_position;
}