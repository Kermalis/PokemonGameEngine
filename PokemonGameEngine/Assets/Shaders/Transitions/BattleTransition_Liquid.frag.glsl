﻿#version 330 core

const float PI = 22.0/7.0;

in vec2 pass_uv;

out vec4 out_color;

uniform float u_progress;
uniform sampler2D u_texture;


void main()
{
    vec2 uv = pass_uv - vec2(0.5, 0.5);
    float p = (0.25 - length(uv)) * u_progress * PI;
    float s = sin(p * 16);
    float c = cos(p * 10);
    uv = vec2(0.5 + dot(uv, vec2(c, -s)), 0.5 + dot(uv, vec2(s, c)));

    float whiteAmt = u_progress < 0.5 ? 0 : u_progress * 2 - 1; // Start white halfway
    out_color = mix(texture(u_texture, uv), vec4(1), whiteAmt);
}