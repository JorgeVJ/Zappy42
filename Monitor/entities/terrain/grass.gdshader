shader_type spatial;
render_mode cull_disabled, unshaded;

uniform sampler2D grass_texture : hint_default_white;

// Gust zone — a circular influence that sweeps across the map
uniform float map_width      = 10.0;
uniform float map_depth      = 10.0;
uniform float travel_speed   = 4.0;
uniform float gust_radius    = 5.0;
uniform float gust_strength  = 0.35;
uniform float wind_dir_x     = 1.0;
uniform float wind_dir_z     = 0.15;

// Very subtle ambient life
uniform float ambient_strength = 0.008;
uniform float ambient_speed    = 1.2;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

    float gust_x = mod(TIME * travel_speed, map_width + gust_radius * 2.0) - gust_radius;
    float gust_z = sin(TIME * 0.35) * map_depth * 0.4 + map_depth * 0.5;

    float dist      = distance(world_pos.xz, vec2(gust_x, gust_z));
    float influence = smoothstep(gust_radius, gust_radius * 0.05, dist);

    float sway = UV.y * influence * gust_strength;
    VERTEX.x += sway * wind_dir_x;
    VERTEX.z += sway * wind_dir_z;

    float tremor = UV.y * ambient_strength
                 * sin(TIME * ambient_speed + world_pos.x * 3.7 + world_pos.z * 2.3);
    VERTEX.x += tremor;
}

void fragment() {
    vec4 tex = texture(grass_texture, UV);
    if (tex.a < 0.5) discard;
    ALBEDO = tex.rgb;
}
