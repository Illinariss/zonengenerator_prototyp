shader_type canvas_item;

uniform float pulse_speed = 0.4;
uniform float pulse_strength = 0.2;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);

    // Zeit hoch und runter zwischen 0..1..0
    float t = mod(TIME * pulse_speed, 1.0);
    float pingpong = abs(1.0 - 2.0 * t); // ergibt 0→1→0

    float pulse = 1.0 - pingpong * pulse_strength;

    tex.rgb *= pulse;
    COLOR = vec4(tex.rgb, tex.a);
}

