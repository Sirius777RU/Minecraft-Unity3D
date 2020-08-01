using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class TerrainChunkObject : MonoBehaviour {
    private TerrainChunk chunk = null;
    
    public Mesh mesh;
    
    public TerrainChunk Chunk { get => chunk; set => chunk = value; }

    public void BuildMesh(TerrainChunk chunk)
    {
        mesh = new Mesh();
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        this.chunk = chunk;

        meshFilter.mesh.vertices = chunk.getVerts().ToArray();
        meshFilter.mesh.triangles = chunk.getTris().ToArray();
        meshFilter.mesh.uv = chunk.getUVs().ToArray();

        meshFilter.mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = meshFilter.mesh;
    }

    public void UpdateChunk() {
        //chunk.RefreshBlocks();
        chunk.UpdateTrig();
        BuildMesh(chunk);
        this.name = this.name + " (u)";
    }

    private void OnDestroy()
    {
        Destroy(mesh);
    }
}