#version 330 core

layout(location = 0) in vec2 in_position;

out vec2 pass_uv;


void main()
{
    float glX = in_position.x * 2 - 1; // (0 => 1) to (-1 => 1)
    float glY = in_position.y * -2 + 1; // (0 => 1) to (1 => -1)
    gl_Position = vec4(glX, glY, 0, 1);

    pass_uv.x = in_position.x;
    pass_uv.y = 1 - in_position.y; // Flip y
}