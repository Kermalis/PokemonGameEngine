#version 330 core

const int MAX_LIGHTS = 4;
const float AMBIENT_LIGHTING = 0.85;
const float SHADE_AMT = 0.6;
const float SHADOW_RENDER_DISTANCE = 50.0;

in vec2 pass_uv;
in vec3 pass_shadowPosData; // xy stores UV, z stores dist to light
in vec3 pass_posRelativeToCam;
in vec3 pass_normal;
in vec3 pass_vecToLight[MAX_LIGHTS];
in vec3 pass_vecToCamera;

out vec4 out_color;

uniform sampler2D diffuseTexture1;
uniform sampler2D shadowColorTexture;
uniform sampler2D shadowDepthTexture;
uniform uint numLights;
uniform vec3 lightColor[MAX_LIGHTS];
uniform vec3 lightAttenuation[MAX_LIGHTS];
uniform float shineDamper;
uniform float specularReflectivity;


// Calculates the lighting for each light
vec3 diffuseLighting(vec3 toLightVector, vec3 lightColor, float attenuation, vec3 normal)
{
    float brightness = max(dot(toLightVector, normal), 0);
    return brightness * lightColor / attenuation;
}
vec3 specularLighting(vec3 toCamVector, vec3 toLightVector, vec3 lightColor, float attenuation, vec3 normal)
{
    float specularFactor = max(dot(reflect(-toLightVector, normal), toCamVector), 0);
    specularFactor = pow(specularFactor, shineDamper);
    return specularFactor * specularReflectivity * lightColor / attenuation;
}
float shadowCalc()
{
    float nearest = texture(shadowDepthTexture, pass_shadowPosData.xy).r;
    float shadeFactor = 1;
    if (pass_shadowPosData.z > nearest)
    {
        float distToCam = length(pass_posRelativeToCam);
        distToCam -= SHADOW_RENDER_DISTANCE;
        shadeFactor -= clamp(1 - distToCam, 0, 1) * SHADE_AMT * texture(shadowColorTexture, pass_shadowPosData.xy).r; // r is the opacity of the BattleSprite
    }
    return shadeFactor;
}

void main()
{
    // Get the color of this pixel from the texture
    vec4 texel = texture(diffuseTexture1, pass_uv);
    if (texel.a < 0.5)
    {
        discard;
    }
    
    // Calculate lighting
    vec3 totalDiffuse = vec3(0);
    vec3 totalSpecular = vec3(0);
    for (uint i = 0u; i < numLights; i++)
    {
        vec3 toLightVector = pass_vecToLight[i];

        // Calculate attenuation before toLightVector is normalized
        float dist = length(toLightVector);
        vec3 atten = lightAttenuation[i];
        float attenFactor = atten.x + (atten.y * dist) + (atten.z * dist * dist);
        
        toLightVector = normalize(toLightVector); // Normalize vector to light for diffuse/specular calculations
        vec3 lColor = lightColor[i];

        totalDiffuse += diffuseLighting(toLightVector, lColor, attenFactor, pass_normal);
        totalSpecular += specularLighting(pass_vecToCamera, toLightVector, lColor, attenFactor, pass_normal);
    }
    totalDiffuse = max(totalDiffuse, AMBIENT_LIGHTING) * shadowCalc(); // Ambient lighting
    
    // Calculate the final color of this pixel
    out_color = vec4(totalDiffuse, 1) * texel + vec4(totalSpecular, 1);
}