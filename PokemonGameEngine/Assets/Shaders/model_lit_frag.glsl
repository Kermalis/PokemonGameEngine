#version 330 core

const int MAX_LIGHTS = 4;
const float AMBIENT_LIGHTING = 0.9;

in vec2 TexCoords;
in vec3 Normal;
in vec3 VecToLight[MAX_LIGHTS];
in vec3 VecToCamera;

out vec4 finalColor;

uniform sampler2D texture_diffuse1;
uniform uint numLights;
uniform vec3 lightColor[MAX_LIGHTS];
uniform vec3 lightAttenuation[MAX_LIGHTS];
uniform float shineDamper;
uniform float reflectivity;

void main()
{
    // Get the color of this pixel from the texture
    vec4 texel = texture(texture_diffuse1, TexCoords);
    if (texel.a < 0.5)
    {
        discard;
    }

    // Apply lighting
    vec3 norm = normalize(Normal);
    vec3 camVector = normalize(VecToCamera);

    vec3 totalDiffuse = vec3(0.0);
    vec3 totalSpecular = vec3(0.0);
    for (uint i = 0u; i < numLights; i++)
    {
        vec3 lightVector = VecToLight[i];
        float dist = length(lightVector); // Get distance before it's normalized
        lightVector = normalize(lightVector);

        float brightness = max(dot(norm, lightVector), 0.0);
        float specularFactor = max(dot(reflect(-lightVector, norm), camVector), 0.0);
        float dampedFactor = pow(specularFactor, shineDamper);

        vec3 lcolor = lightColor[i];
        vec3 atten = lightAttenuation[i];
        float attenFactor = atten.x + (atten.y * dist) + (atten.z * dist * dist);
        totalDiffuse += (brightness * lcolor) / attenFactor;
        totalSpecular += (dampedFactor * reflectivity * lcolor) / attenFactor;
    }
    totalDiffuse = max(totalDiffuse, AMBIENT_LIGHTING); // Ambient lighting

    finalColor = vec4(totalDiffuse, 1.0) * texel + vec4(totalSpecular, 1.0); // Final value
}