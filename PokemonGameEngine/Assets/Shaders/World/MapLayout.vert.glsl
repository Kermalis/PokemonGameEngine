#version 330 core

layout(location = 0) in vec2 in_position;
layout(location = 1) in ivec2 in_instancedTranslation;
layout(location = 2) in float in_instancedTexture;

out vec3 pass_uvw;

uniform ivec2 blockSize;
uniform ivec2 viewportSize;


vec2 RelToGL(vec2 v)
{
    return vec2(v.x * 2 - 1, v.y * -2 + 1);
}

void main()
{
    vec2 relPos = ((in_position * blockSize) + in_instancedTranslation) / viewportSize; // Abs to Rel
    gl_Position = vec4(RelToGL(relPos), 0, 1);

    pass_uvw.x = in_position.x;
    pass_uvw.y = 1 - in_position.y; // Flip y
    pass_uvw.z = in_instancedTexture;
}