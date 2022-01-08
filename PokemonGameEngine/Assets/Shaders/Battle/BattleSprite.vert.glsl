#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_uv;

uniform mat4 transformViewProjection;


void main()
{
    float x = in_position.x * 0.5; // (-1 => 1) to (-0.5 => 0.5) (center align)
    float y = (in_position.y + 1) * 0.5; // (1 => -1) to (1 => 0) (bottom align)
    gl_Position = transformViewProjection * vec4(x, y, 0, 1);

    pass_uv.x = (in_position.x * 0.5) + 0.5; // (-1 => 1) to (0 => 1)
    pass_uv.y = (in_position.y * -0.5) + 0.5; // (1 => -1) to (0 => 1)
}