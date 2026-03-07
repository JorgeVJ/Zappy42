shader_type spatial;

uniform float tile_size = 1.0;
uniform float line_width = 0.05;

uniform vec4 line_color : source_color = vec4(0.0,0.0,0.0,1.0);
uniform vec4 ground_color : source_color = vec4(0.4,0.8,0.4,1.0);

varying vec3 world_pos;

void vertex()
{
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment()
{
    vec2 grid = fract(world_pos.xz / tile_size);

    float line_x = step(grid.x, line_width) + step(1.0 - grid.x, line_width);
    float line_y = step(grid.y, line_width) + step(1.0 - grid.y, line_width);

    float line = max(line_x, line_y);

    ALBEDO = mix(ground_color.rgb, line_color.rgb, line);
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
