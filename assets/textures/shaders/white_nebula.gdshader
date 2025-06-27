shader_type canvas_item;
render_mode unshaded, blend_premul_alpha;

// Was?  Weißer Nebel für den Kartenrand.
// Wie?  Zwei Rauschlayer werden animiert und gemischt.
// Warum?  Damit beim Scrollen kein leerer Hintergrund sichtbar wird.

uniform float scale = 4.0;   // Dichte des Musters
uniform float speed = 0.1;   // Animationsgeschwindigkeit
uniform float alpha = 0.6;   // Deckkraft

float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

float noise2d(vec2 st) {
    vec2 i = floor(st);
    vec2 f = fract(st);
    float a = rand(i);
    float b = rand(i + vec2(1.0, 0.0));
    float c = rand(i + vec2(0.0, 1.0));
    float d = rand(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(a, b, u.x) +
           (c - a) * u.y * (1.0 - u.x) +
           (d - b) * u.x * u.y;
}

void fragment() {
    vec2 uv = UV * scale + vec2(TIME * speed, TIME * speed * 0.5);
    float n1 = noise2d(uv);
    float n2 = noise2d(uv * 2.0 + 10.0);
    float n = mix(n1, n2, 0.5);
    float intensity = pow(n, 1.5);
    COLOR = vec4(vec3(intensity), intensity * alpha);
}
