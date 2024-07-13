#version 140
#define HAS_MOD
#define HAS_DFDX
#define HAS_FLOAT_TEXTURES
#define HAS_SRGB
#define HAS_UNIFORM_BUFFERS
#define FRAGMENT_SHADER

// -- Utilities Start --

// It's literally just called the Z-Library for alphabetical ordering reasons.
//  - 20kdc

// -- varying/attribute/texture2D --

#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#ifdef VERTEX_SHADER
#define varying out
#define attribute in
#else
#define varying in
#define attribute in
#define gl_FragColor colourOutput
out highp vec4 colourOutput;
#endif
#endif

#ifndef NO_ARRAY_PRECISION
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#else
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#endif

// -- shadow depth --

// If float textures are supported, puts the values in the R/G fields.
// This assumes RG32F format.
// If float textures are NOT supported.
// This assumes RGBA8 format.
// Operational range is "whatever works for FOV depth"
highp vec4 zClydeShadowDepthPack(highp vec2 val) {
#ifdef HAS_FLOAT_TEXTURES
    return vec4(val, 0.0, 1.0);
#else
    highp vec2 valH = floor(val);
    return vec4(valH / 255.0, val - valH);
#endif
}

// Inverts the previous function.
highp vec2 zClydeShadowDepthUnpack(highp vec4 val) {
#ifdef HAS_FLOAT_TEXTURES
    return val.xy;
#else
    return (val.xy * 255.0) + val.zw;
#endif
}

// -- srgb/linear conversion core --

highp vec4 zFromSrgb(highp vec4 sRGB)
{
    highp vec3 higher = pow((sRGB.rgb + 0.055) / 1.055, vec3(2.4));
    highp vec3 lower = sRGB.rgb / 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.04045));
    return vec4(mix(lower, higher, s), sRGB.a);
}

highp vec4 zToSrgb(highp vec4 sRGB)
{
    highp vec3 higher = (pow(sRGB.rgb, vec3(0.41666666666667)) * 1.055) - 0.055;
    highp vec3 lower = sRGB.rgb * 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.0031308));
    return vec4(mix(lower, higher, s), sRGB.a);
}

// -- uniforms --

#ifdef HAS_UNIFORM_BUFFERS
layout (std140) uniform projectionViewMatrices
{
    highp mat3 projectionMatrix;
    highp mat3 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    highp vec2 SCREEN_PIXEL_SIZE;
    highp float TIME;
};
#else
uniform highp mat3 projectionMatrix;
uniform highp mat3 viewMatrix;
uniform highp vec2 SCREEN_PIXEL_SIZE;
uniform highp float TIME;
#endif

uniform sampler2D TEXTURE;
uniform highp vec2 TEXTURE_PIXEL_SIZE;

// -- srgb emulation --

#ifdef HAS_SRGB

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    return texture2D(tex, uv);
}

highp vec4 zAdjustResult(highp vec4 col)
{
    return col;
}
#else
uniform lowp vec2 SRGB_EMU_CONFIG;

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    highp vec4 col = texture2D(tex, uv);
    if (SRGB_EMU_CONFIG.x > 0.5)
    {
        return zFromSrgb(col);
    }
    return col;
}

highp vec4 zAdjustResult(highp vec4 col)
{
    if (SRGB_EMU_CONFIG.y > 0.5)
    {
        return zToSrgb(col);
    }
    return col;
}
#endif

highp vec4 zTexture(highp vec2 uv)
{
    return zTextureSpec(TEXTURE, uv);
}

// -- color --

// Grayscale function for the ITU's Rec BT-709. Primarily intended for HDTVs, but standard sRGB monitors are coincidentally extremely close.
highp float zGrayscale_BT709(highp vec3 col) {
    return dot(col, vec3(0.2126, 0.7152, 0.0722));
}

// Grayscale function for the ITU's Rec BT-601, primarily intended for SDTV, but amazing for a handful of niche use-cases.
highp float zGrayscale_BT601(highp vec3 col) {
    return dot(col, vec3(0.299, 0.587, 0.114));
}

// If you don't have any reason to be specifically using the above grayscale functions, then you should default to this.
highp float zGrayscale(highp vec3 col) {
    return zGrayscale_BT709(col);
}

// -- noise --

//zRandom, zNoise, and zFBM are derived from https://godotshaders.com/snippet/2d-noise/ and https://godotshaders.com/snippet/fractal-brownian-motion-fbm/
highp vec2 zRandom(highp vec2 uv){
    uv = vec2( dot(uv, vec2(127.1,311.7) ),
               dot(uv, vec2(269.5,183.3) ) );
    return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

highp float zNoise(highp vec2 uv) {
    highp vec2 uv_index = floor(uv);
    highp vec2 uv_fract = fract(uv);

    highp vec2 blur = smoothstep(0.0, 1.0, uv_fract);

    return mix( mix( dot( zRandom(uv_index + vec2(0.0,0.0) ), uv_fract - vec2(0.0,0.0) ),
                     dot( zRandom(uv_index + vec2(1.0,0.0) ), uv_fract - vec2(1.0,0.0) ), blur.x),
                mix( dot( zRandom(uv_index + vec2(0.0,1.0) ), uv_fract - vec2(0.0,1.0) ),
                     dot( zRandom(uv_index + vec2(1.0,1.0) ), uv_fract - vec2(1.0,1.0) ), blur.x), blur.y) * 0.5 + 0.5;
}

highp float zFBM(highp vec2 uv) {
    const int octaves = 6;
    highp float amplitude = 0.5;
    highp float frequency = 3.0;
    highp float value = 0.0;

    for(int i = 0; i < octaves; i++) {
        value += amplitude * zNoise(frequency * uv);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}


// -- generative --

// Function that creates a circular gradient. Screenspace shader bread n butter.
highp float zCircleGradient(highp vec2 ps, highp vec2 coord, highp float maxi, highp float radius, highp float dist, highp float power) {
    highp float rad = (radius * ps.y) * 0.001;
    highp float aspectratio = ps.x / ps.y;
    highp vec2 totaldistance = ((ps * 0.5) - coord) / (rad * ps);
    totaldistance.x *= aspectratio;
    highp float length = (length(totaldistance) * ps.y) - dist;
    return pow(clamp(length, 0.0, maxi), power);
}

// -- Utilities End --

varying highp vec2 UV;
varying highp vec2 Pos;
varying highp vec4 VtxModulate;

uniform sampler2D lightMap;



ARRAY_HIGHP float sdSphere( ARRAY_HIGHP vec3 p,  ARRAY_HIGHP float s) {
 return length ( p ) - s ;

}
ARRAY_HIGHP float sdVerticalCapsule( ARRAY_HIGHP vec3 p,  ARRAY_HIGHP float h,  ARRAY_HIGHP float r) {
 p . y -= clamp ( p . y , 0.0 , h ) ;
 return length ( p ) - r ;

}
ARRAY_HIGHP mat2 rot2D( ARRAY_HIGHP float a) {
 highp float c = cos ( a ) ;
 highp float s = sin ( a ) ;
 return mat2 ( c , s , - s , c ) ;

}
ARRAY_HIGHP float smin( ARRAY_HIGHP float a,  ARRAY_HIGHP float b,  ARRAY_HIGHP float k) {
 highp float h = max ( k - abs ( a - b ) , 0.0 ) / k ;
 return min ( a , b ) - h * h * h * k * ( 1.0 / 6.0 ) ;

}
ARRAY_HIGHP vec4 map( ARRAY_HIGHP vec3 p) {
 highp vec3 speherePos = vec3 ( 0 , 0 , 0 ) ;
 highp vec3 capsulePos = vec3 ( 0 , - 0.1 , 0 ) ;
 highp vec3 sphereColor = vec3 ( 0.94 , 0.45 , 0.15 ) ;
 highp vec3 capsuleColor = vec3 ( 0.31 , 0.78 , 0.47 ) ;
 highp vec3 q = p ;
 q . xz *= rot2D ( TIME ) ;
 q . xy *= rot2D ( 0.6 ) ;
 highp float sphere = sdSphere ( q - speherePos , 0.76 ) ;
 highp float capsule = sdVerticalCapsule ( q - capsulePos , 1.0 , 0.1 ) ;
 speherePos . xz *= rot2D ( TIME ) ;
 return vec4 ( smin ( capsule , sphere , 0.1 ) , capsule < sphere ? capsuleColor : sphereColor ) ;

}


void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR;

    lowp vec3 lightSample = texture2D(lightMap, Pos).rgb;

     highp vec2 ratio = 1.0 / SCREEN_PIXEL_SIZE . x ;
 highp vec2 textureSize = 1.0 / TEXTURE_PIXEL_SIZE ;
 highp vec2 uv = fract ( UV * textureSize / ratio ) * 2 - 1 ;
 highp vec3 ro = vec3 ( 0 , 0 , - 3 ) ;
 highp vec3 rd = normalize ( vec3 ( uv , 1 ) ) ;
 highp vec3 col = vec3 ( 0 ) ;
 highp float op = 0.0 ;
 highp float t = 0.0 ;
 for ( int i = 0 ;
 i < 80 ;
 i ++ ) {
 highp vec3 p = ro + rd * t ;
 highp vec4 d = map ( p ) ;
 col = d . yzw ;
 t += d . x ;
 if ( d . x < 0.001 ) {
 op = 1.0 ;
 break ;
 }
 if ( t > 100.0 ) break ;
 }
 col = vec3 ( t * 0.2 ) * col ;
 COLOR = vec4 ( uv , 0.0 , 1.0 ) ;


    gl_FragColor = zAdjustResult(COLOR * VtxModulate * vec4(lightSample, 1.0));
}
