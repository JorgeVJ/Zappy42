shader_type spatial;
// Transparente, no escribe profundidad (deja ver el terreno sumergido a través del agua).
render_mode cull_back, depth_draw_never, specular_schlick_ggx;

// --- Colores ---
uniform vec4 deep_color : source_color    = vec4(0.02, 0.16, 0.30, 1.0);
uniform vec4 shallow_color : source_color = vec4(0.10, 0.46, 0.58, 1.0);
uniform vec4 foam_color : source_color    = vec4(0.86, 0.95, 1.0, 1.0);
uniform vec4 horizon_color : source_color = vec4(0.55, 0.68, 0.78, 1.0);

// --- Caústicas (líneas brillantes en movimiento) ---
uniform float caustic_scale    = 0.55;
uniform float caustic_speed    = 0.45;
uniform float caustic_strength = 0.55;
uniform float caustic_sharpness = 4.0;

// --- Normales animadas (specular en movimiento) ---
uniform float normal_scale    = 1.1;
uniform float normal_speed     = 0.35;
uniform float normal_strength = 0.30;

// --- Costa / aguas someras (vía DEPTH_TEXTURE) ---
uniform bool  use_depth_shore = true;
uniform float shore_depth     = 1.6;   // unidades de mundo del degradado somero→profundo
uniform float foam_width      = 0.35;  // ancho de la banda de espuma en la orilla
uniform float foam_speed      = 1.5;

// --- Reflejo de horizonte / transparencia ---
uniform float fresnel_strength = 0.40;
uniform float base_alpha       = 0.92;
uniform float shallow_alpha    = 0.55;

uniform sampler2D depth_tex : hint_depth_texture;

varying vec3 world_pos;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Ruido de valor (misma familia que terrain.gdshader / grass.gdshader)
float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(hash(i),               hash(i + vec2(1.0, 0.0)), u.x),
		mix(hash(i + vec2(0.0,1.0)), hash(i + vec2(1.0, 1.0)), u.x),
		u.y);
}

// Distancia celular (voronoi) — base de las líneas de caústica
float voro(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	float md = 1.0;
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 g = vec2(float(x), float(y));
			vec2 o = vec2(hash(i + g), hash(i + g + 17.0));
			vec2 r = g + o - f;
			md = min(md, dot(r, r));
		}
	}
	return sqrt(md);
}

// Dos capas de voronoi scrolleando en direcciones distintas → red de líneas brillantes
float caustics(vec2 p, float t) {
	float a = voro(p * caustic_scale          + vec2(t,        t * 0.5));
	float b = voro(p * caustic_scale * 1.7    - vec2(t * 0.7,  t * 0.9));
	return pow(1.0 - min(a, b), caustic_sharpness);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 wp = world_pos.xz;

	// --- Normal perturbada (specular animado) ---
	vec2 np = wp * normal_scale + vec2(TIME * normal_speed, TIME * normal_speed * 0.7);
	float e = 0.18;
	float nC = vnoise(np);
	float nX = vnoise(np + vec2(e, 0.0));
	float nZ = vnoise(np + vec2(0.0, e));
	vec3 wnormal = normalize(vec3((nC - nX) * normal_strength, 1.0, (nC - nZ) * normal_strength));
	NORMAL = normalize((VIEW_MATRIX * vec4(wnormal, 0.0)).xyz);

	// --- Costa / profundidad del agua (DEPTH_TEXTURE) ---
	float shallowness = 0.0; // 0 = aguas profundas/mar abierto, 1 = justo en la orilla
	float foam = 0.0;
	if (use_depth_shore) {
		float raw = texture(depth_tex, SCREEN_UV).r;
		vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, raw);
		vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
		view.xyz /= view.w;
		float scene_depth = -view.z;     // distancia lineal al terreno tras el agua
		float frag_depth  = -VERTEX.z;   // VERTEX en fragment = posición en view-space
		float water_depth = scene_depth - frag_depth;

		shallowness = 1.0 - clamp(water_depth / shore_depth, 0.0, 1.0);

		float fband  = 1.0 - clamp(water_depth / foam_width, 0.0, 1.0);
		float wobble = vnoise(wp * 6.0 + vec2(TIME * foam_speed, 0.0));
		foam = smoothstep(0.45, 1.0, fband * (0.6 + 0.6 * wobble));
	}

	// --- Composición de color ---
	vec3 col = mix(deep_color.rgb, shallow_color.rgb, shallowness);
	float caust = caustics(wp, TIME * caustic_speed) * caustic_strength * mix(0.6, 1.0, shallowness);
	col += caust * shallow_color.rgb;

	// Fresnel: tinte de horizonte en ángulos rasantes → funde el mar lejano con el cielo
	float fres = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 5.0);
	col = mix(col, horizon_color.rgb, fres * fresnel_strength);

	// Espuma de orilla por encima de todo
	col = mix(col, foam_color.rgb, foam);

	ALBEDO    = col;
	METALLIC  = 0.0;
	ROUGHNESS = 0.08;
	SPECULAR  = 0.5;

	float alpha = mix(base_alpha, shallow_alpha, shallowness);
	ALPHA = max(alpha, foam); // la espuma es opaca
}
