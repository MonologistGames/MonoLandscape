using Godot;

namespace MonoLandscape.Utilities;

public static class MeshGenerator
{
    public static Mesh GeneratePatch(int size, float resolution)
    {
        var arrays = new Godot.Collections.Array();
        var vertices = new Vector3[size * size];
        var indices = new int[(size - 1) * (size - 1) * 6];
        var index = 0;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var vertex = new Vector3(x * resolution, 0, y * resolution);
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
                indices[index++] = (y + 1) * size + x;
                indices[index++] = y * size + x + 1;
                indices[index++] = (y + 1) * size + x;
                indices[index++] = (y + 1) * size + x + 1;
                indices[index++] = y * size + x + 1;
            }
        }
        arrays.Resize((int)RenderingServer.ArrayType.Max);
        arrays[(int) RenderingServer.ArrayType.Vertex] = vertices;
        arrays[(int) RenderingServer.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);

        return mesh;
    }
}