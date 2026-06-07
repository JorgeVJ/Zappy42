shader_type spatial;

uniform float tile_size = 10.0;
uniform float line_width = 0.01;

uniform vec4 line_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 ground_color : source_color = vec4(0.4, 0.8, 0.4, 1.0);
uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float pulse_speed = 3.0;
uniform bool has_selection = false;
uniform vec2 selected_tile = vec2(-1.0, -1.0);

varying vec3 world_pos;

void vertex()
{
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment()
{
    // Lines: only visible when a tile is selected
    float eff_width = has_selection ? line_width : 0.0;
    vec2 grid = fract(world_pos.xz / tile_size);
    float line_x = step(grid.x, eff_width) + step(1.0 - grid.x, eff_width);
    float line_y = step(grid.y, eff_width) + step(1.0 - grid.y, eff_width);
    float line = max(line_x, line_y);

    // Check if this pixel belongs to the selected tile
    vec2 tile_coord = floor(world_pos.xz / tile_size);
    bool in_selected = has_selection && (tile_coord == selected_tile);

    // Pulse: oscillates between 0.0 and 0.6 using TIME
    float pulse = in_selected ? (0.3 + 0.3 * sin(TIME * pulse_speed)) : 0.0;

    // Compose: ground → grid lines → highlight pulse
    vec3 base = mix(ground_color.rgb, line_color.rgb, line);
    ALBEDO = mix(base, highlight_color.rgb, pulse);
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
