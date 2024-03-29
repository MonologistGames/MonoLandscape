using Godot;

namespace MonoLandscape.Utilities;

public static class MeshGenerator
{
    public static Rid GeneratePatch(int size, float resolution)
    {
        var arrays = new Godot.Collections.Array();
        var vertices = new Vector3[size * size];
        var indices = new int[(size - 1) * (size - 1) * 6];
        var index = 0;
        var start = new Vector3((size - 1) * resolution * 0.5f, 0, (size - 1) * resolution * 0.5f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var vertex = new Vector3(x * resolution, 0, y * resolution) - start;
                vertices[index] = vertex;
                index++;
            }
        }

        index = 0;
        for (var y = 0; y < size - 1; y++)
        {
            for (var x = 0; x < size - 1; x++)
            {
                indices[index++] = y * size + x;
                indices[index++] = (y + 1) * size + x + 1;
                indices[index++] = (y + 1) * size + x;

                indices[index++] = y * size + x;
                indices[index++] = y * size + x + 1;
                indices[index++] = (y + 1) * size + x + 1;
            }
        }

        arrays.Resize((int)RenderingServer.ArrayType.Max);
        arrays[(int)RenderingServer.ArrayType.Vertex] = vertices;
        arrays[(int)RenderingServer.ArrayType.Index] = indices;
        var mesh = RenderingServer.Singleton.MeshCreate();
        RenderingServer.Singleton.MeshAddSurfaceFromArrays(mesh, RenderingServer.PrimitiveType.Triangles, arrays);
        RenderingServer.Singleton.MeshSetCustomAabb(mesh,
            new Aabb(Vector3.One * ((size - 1) * -0.5f), Vector3.One * ((size - 1) * 0.5f)));

        return mesh;
    }
}