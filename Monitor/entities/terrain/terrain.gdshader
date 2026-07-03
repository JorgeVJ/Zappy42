shader_type spatial;
render_mode world_vertex_coords;

// Exportados
uniform float tile_size = 10.0;
uniform float line_width = 0.01;
uniform bool has_selection = false;
uniform vec2 selected_tile = vec2(-1.0, -1.0);

// Locales
uniform vec4 line_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 ground_color_b : source_color = vec4(0.2, 0.9, 0.2, 1.0);
uniform vec4 ground_color : source_color = vec4(0.18, 0.82, 0.12, 1.0);
uniform float color_variation = 0.82;
uniform float noise_scale = 1;
uniform vec4 sand_color : source_color = vec4(0.851, 0.784, 0.451, 1.0); // arena cálida
uniform float sand_height = 0.0;   // world-Y por debajo del cual el suelo es arena
uniform float sand_blend  = 1.2;   // ancho de la transición pasto→arena (unidades de mundo)
uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float pulse_speed = 3.0;

varying vec3 world_pos;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float value_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(
        mix(hash(i),                  hash(i + vec2(1.0, 0.0)), u.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
        u.y
    );
}

vec3 compute_varied_ground(vec2 world_xz) {
    float n = value_noise(world_xz * noise_scale) * color_variation;
    return mix(ground_color.rgb, ground_color_b.rgb, n);
}

vec3 apply_sand(vec3 ground, float world_y) {
    float sand_factor = 1.0 - smoothstep(sand_height - sand_blend, sand_height, world_y + 1.2);
    return mix(ground, sand_color.rgb, sand_factor);
}

float compute_grid_line(vec2 world_xz) {
    vec2 grid = fract(world_xz / tile_size);
    float line_x = step(grid.x, line_width) + step(1.0 - grid.x, line_width);
    float line_y = step(grid.y, line_width) + step(1.0 - grid.y, line_width);
    return max(line_x, line_y);
}

float compute_pulse(vec2 world_xz) {
    vec2 tile_coord = floor(world_xz / tile_size);
    bool in_selected = (tile_coord == selected_tile);
    return in_selected ? (0.3 + 0.3 * sin(TIME * pulse_speed)) : 0.0;
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment()
{
    vec3 varied_ground = compute_varied_ground(world_pos.xz);
    vec3 ground_with_sand = apply_sand(varied_ground, world_pos.y);

    float line = 0.0;
    float pulse = 0.0;
    if (has_selection) {
        line = compute_grid_line(world_pos.xz);
        pulse = compute_pulse(world_pos.xz);
    }

    vec3 base = mix(ground_with_sand, line_color.rgb, line);
    ALBEDO = mix(base, highlight_color.rgb, pulse);
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
