shader_type spatial;
render_mode unshaded,diffuse_burley, specular_schlick_ggx, world_vertex_coords;

uniform sampler2D heightmap:filter_nearest,repeat_disable;
uniform float max_height;
uniform float region_size;
instance uniform int lod;
uniform ivec2 region_pos;

const vec3 colors[6] = {vec3(1.0,0,0),vec3(0,1.0,0),vec3(0,0,1.0),vec3(1.0,1.0,0),vec3(0,1.0,1.0),vec3(1.0,0,1.0)};
varying vec3 pos;

void vertex()
{
    // TODO: Use CDLOD-like method to solve T-junctions
	pos = VERTEX;
	vec2 uv = VERTEX.xz - vec2(region_pos) * region_size;
	float h = texture(heightmap, uv/region_size).r;
	uv /= region_size;
	//VERTEX.xz += vec2(region_pos) *20.0;
	VERTEX.y = h * max_height;
	COLOR = vec4(uv.x,0.0,uv.y,1.0);
}

void fragment()
{
	//ALBEDO = vec3(region_size);
	float c = texture(heightmap, fract(pos.xz/(region_size))).r;
	ALBEDO = colors[lod] * c / 2.0;
	//ALBEDO = vec3(c / 5.0);
}