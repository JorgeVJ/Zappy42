shader_type spatial;
render_mode cull_back, depth_draw_never, diffuse_lambert, specular_schlick_ggx;

// --- Colores principales ---
uniform vec4 water_dark : source_color    = vec4(0.01, 0.22, 0.49, 0.85);
uniform vec4 water_light : source_color   = vec4(0.04, 0.47, 0.74, 0.40);
uniform vec4 line_color : source_color    = vec4(0.51, 0.83, 0.98, 1.0);
uniform vec4 foam_color : source_color    = vec4(0.95, 0.98, 1.00, 1.0); // Color de la espuma

// --- Ajustes del patrón (AQUÍ CONTROLAS LA ESCALA) ---
uniform float pattern_scale : hint_range(0.01, 2.0) = 1.42; // Menor valor = celdas más grandes
uniform float pattern_speed : hint_range(0.0, 2.0) = 0.15;
uniform float line_thickness : hint_range(0.001, 0.1) = 0.042;

// --- Distorsión por Ruido Orgánico ---
uniform float noise_strength : hint_range(0.0, 2.0) = 1.45;
uniform float noise_scale : hint_range(0.1, 5.0) = 2;

// --- Profundidad, Transparencia y Espuma ---
uniform float max_depth : hint_range(0.1, 10.0) = 2.5; 
uniform float foam_cutoff : hint_range(0.01, 2.0) = 0.15; // Ancho de la espuma en las rocas/orilla
uniform float foam_wobble : hint_range(0.0, 1.0) = 0.8;   // Qué tan irregular es el borde de la espuma

uniform sampler2D depth_tex : hint_depth_texture, filter_linear_mipmap;

varying vec3 world_pos;

vec2 hash2(vec2 p) {
	p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
	return fract(sin(p) * 43758.5453123);
}

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(41.0, 289.0))) * 43758.5453);
}

float soft_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i + vec2(0.0,0.0)), hash(i + vec2(1.0,0.0)), u.x),
			   mix(hash(i + vec2(0.0,1.0)), hash(i + vec2(1.0,1.0)), u.x), u.y);
}

vec2 noise_distort(vec2 p, float t) {
	float n1 = soft_noise(p * noise_scale + vec2(t, t * 0.7));
	float n2 = soft_noise(p * noise_scale * 1.5 - vec2(t * 0.5, t));
	return vec2(n1, n2) * 2.0 - 1.0;
}

float voronoi_lines(vec2 p) {
	vec2 n = floor(p);
	vec2 f = fract(p);
	
	vec2 mg, mr;
	float md = 8.0;
	for (int j = -1; j <= 1; j++) {
		for (int i = -1; i <= 1; i++) {
			vec2 g = vec2(float(i), float(j));
			vec2 o = hash2(n + g);
			o = 0.5 + 0.5 * sin(TIME * pattern_speed * 1.8 + 6.2831 * o);
			vec2 r = g + o - f;
			float d = dot(r, r);
			if (d < md) {
				md = d; mr = r; mg = g;
			}
		}
	}
	
	md = 8.0;
	for (int j = -1; j <= 1; j++) {
		for (int i = -1; i <= 1; i++) {
			vec2 g = mg + vec2(float(i), float(j));
			vec2 o = hash2(n + g);
			o = 0.5 + 0.5 * sin(TIME * pattern_speed * 1.8 + 6.2831 * o);
			vec2 r = g + o - f;
			if (dot(mr - r, mr - r) > 0.00001) {
				md = min(md, dot(0.5 * (mr + r), normalize(r - mr)));
			}
		}
	}
	return md;
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// 1. Lectura de profundidad de la escena
	float raw_depth = texture(depth_tex, SCREEN_UV).r;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, raw_depth);
	vec4 view_space = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view_space.xyz /= view_space.w;
	float depth_water = -view_space.z - (-VERTEX.z);
	
	float depth_factor = clamp(depth_water / max_depth, 0.0, 1.0);
	
	// 2. Coordenadas y distorsión por ruido
	vec2 uv = world_pos.xz * pattern_scale;
	vec2 distort = noise_distort(uv, TIME * pattern_speed);
	uv += distort * noise_strength * 0.15;
	
	// 3. Cálculo de las líneas del agua
	float edge_dist = voronoi_lines(uv);
	float line_width = line_thickness * (1.0 + depth_factor * 0.5);
	float line_mask = smoothstep(line_width, line_width - (fwidth(edge_dist) * 1.5), edge_dist);
	
	// 4. Sistema de Espuma de Orilla Estilizada
	// Generamos una variación orgánica en el borde usando el mismo ruido
	float noise_foam = soft_noise(world_pos.xz * 1.5 + vec2(TIME * pattern_speed * 2.0));
	float foam_depth_limit = foam_cutoff + (noise_foam * foam_wobble * 0.15);
	
	// Máscara binaria/nítida para la espuma tipo Cel-shading
	float foam_mask = step(depth_water, foam_depth_limit);
	
	// 5. Mezcla de colores e iluminación básica
	vec3 water_color = mix(water_light.rgb, water_dark.rgb, depth_factor);
	vec3 final_color = mix(water_color, line_color.rgb, line_mask);
	
	// Aplicamos la espuma densa sobre el color del agua y las líneas
	final_color = mix(final_color, foam_color.rgb, foam_mask);
	
	// Configuración del Alpha final
	float final_alpha = mix(water_light.a, water_dark.a, depth_factor);
	final_alpha = max(final_alpha, line_mask * line_color.a);
	final_alpha = max(final_alpha, foam_mask * foam_color.a); // La espuma es más opaca

	ALBEDO = final_color;
	METALLIC = 0.0;
	ROUGHNESS = 0.15;
	SPECULAR = 0.1;
	ALPHA = final_alpha;
}
