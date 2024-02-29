shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx, world_vertex_coords;

uniform sampler2D heightmap:filter_nearest,repeat_disable;
uniform float max_height;
uniform float region_size;
uniform float vert_gap = 0.5;
uniform int grid_size = 17;
uniform ivec2 region_pos;

instance uniform int lod;
instance uniform ivec4 lod_bias;

const vec3 colors[6] = {vec3(1.0,0,0),vec3(0,1.0,0),vec3(0,0,1.0),vec3(1.0,1.0,0),vec3(0,1.0,1.0),vec3(1.0,0,1.0)};
varying vec3 pos;

int modulo(int a, int b)
{
	return a - (a / b) * b;
}

void vertex()
{
	float gap = float(1 << lod) * vert_gap;
	pos = VERTEX;
	vec2 uv = VERTEX.xz - vec2(region_pos) * region_size;
	COLOR = vec4(colors[lod],1);
	int vert_result = VERTEX_ID / grid_size;
	int vert_modulo = modulo(VERTEX_ID, grid_size);
	if (vert_result == 0 && lod_bias.w > 0) 
	{
		COLOR = vec4(1.0);
		int offset = modulo(vert_modulo, lod_bias.w);
		
	}
	if (vert_result == grid_size - 1 && lod_bias.y > 0) COLOR = vec4(1.0);
	if (vert_modulo == 0 && lod_bias.x > 0) COLOR = vec4(1.0);
	if (vert_modulo == grid_size - 1 && lod_bias.z > 0) COLOR = vec4(1.0);
	float h = texture(heightmap, uv/region_size).r;
	VERTEX.y = h * max_height;
	//VERTEX.xz = uv + vec2(region_pos) * region_size;
	VERTEX.xz += vec2(region_pos) * 20.0;
}

void fragment()
{
	float c = texture(heightmap, fract(pos.xz/(region_size))).r;
	ALBEDO = colors[lod] * c / 2.0;
	ALBEDO = COLOR.rgb;
}