#version 330 core

//include(2d.glsl)

float AbsToRel(int x, uint total)
{
    return float(x) / total;
}
float RelXToGLX(float x)
{
    return (x * 2) - 1;
}
float RelYToGLY(float y)
{
    return (y * -2) + 1;
}
vec2 AbsToRel(ivec2 v, uvec2 res)
{
    return vec2(AbsToRel(v.x, res.x), AbsToRel(v.y, res.y));
}
vec2 RelToGL(vec2 v)
{
    return vec2(RelXToGLX(v.x), RelYToGLY(v.y));
}
vec2 AbsToGL(ivec2 v, uvec2 res)
{
    return RelToGL(AbsToRel(v, res));
}


layout(location = 0) in ivec2 pos;

uniform uvec2 resolution;

void main()
{
    vec2 pos2D = AbsToGL(pos, resolution);
    gl_Position = vec4(pos2D, 0.0, 1.0);
}