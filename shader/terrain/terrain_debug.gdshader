shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx, world_vertex_coords;

uniform sampler2D heightmap:filter_nearest;
uniform float height;
uniform float region_size;
uniform float lod = 1;

/*void vertex()
{
    float h = texture(heightmap, (VERTEX.xz + 0.5) / region_size).r * height;
    VERTEX.y = h;
}*/

void fragment()
{
	ALBEDO = vec3(lod/6.0);
}