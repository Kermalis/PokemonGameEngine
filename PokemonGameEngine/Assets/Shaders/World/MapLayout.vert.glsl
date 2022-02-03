#version 330 core

layout(location = 0) in vec2 in_position;
layout(location = 1) in ivec2 in_instancedTranslation;
layout(location = 2) in float in_instancedTexture;

out vec3 pass_uvw;

uniform ivec2 u_blockSize;
uniform ivec2 u_viewportSize;


void main()
{
    vec2 relPos = ((in_position * u_blockSize) + in_instancedTranslation) / u_viewportSize; // Abs to Rel
    float glX = relPos.x * 2 - 1; // (0 => 1) to (-1 => 1)
    float glY = relPos.y * -2 + 1; // (0 => 1) to (1 => -1)
    gl_Position = vec4(glX, glY, 0, 1);

    pass_uvw.x = in_position.x;
    pass_uvw.y = 1 - in_position.y; // Flip y
    pass_uvw.z = in_instancedTexture;
}