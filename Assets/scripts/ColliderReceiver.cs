using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ColliderReceiver : MonoBehaviourPunCallbacks
{
    public Material wireFrimeMaterial;
    public bool showWireframe;

    private List<Vector3> receivedVertices = new List<Vector3>();
    private List<int> receivedTriangles = new List<int>();
    private int fragmentsReceived = 0;
    private int totalExpectedFragments = 0;

    [PunRPC]
    public void RecibirMeshColliderFragment_RPC(Vector3[] verticesFragment, int[] trianglesFragment, int fragmentIndex, int totalFragments)
    {
        Debug.Log($"Fragmento recibido: {fragmentIndex + 1}/{totalFragments}");

        if (verticesFragment.Length == 0 || trianglesFragment.Length == 0)
        {
            Debug.LogError($"Fragmento {fragmentIndex + 1} contiene datos vac�os.");
            return;
        }

        int maxIndex = Mathf.Max(trianglesFragment);
        if (maxIndex >= receivedVertices.Count + verticesFragment.Length)
        {
            Debug.LogError($"Fragmento {fragmentIndex + 1} tiene �ndices de tri�ngulos fuera de los l�mites.");
            return;
        }

        receivedVertices.AddRange(verticesFragment);
        receivedTriangles.AddRange(trianglesFragment);

        fragmentsReceived++;
        totalExpectedFragments = totalFragments;

        if (fragmentsReceived == totalExpectedFragments)
        {
            ReconstruirMesh();
        }
    }

    private void ReconstruirMesh()
    {
        GameObject newColliderObject = new GameObject("RemoteMeshCollider");
        Mesh mesh = new Mesh();

        // Verificar datos antes de ensamblar
        if (receivedTriangles.Exists(t => t >= receivedVertices.Count || t < 0))
        {
            Debug.LogError("�ndice de tri�ngulo fuera de rango de los v�rtices recibidos.");
            return;
        }

        mesh.vertices = receivedVertices.ToArray();
        mesh.triangles = receivedTriangles.ToArray();

        // Recalcular normales y l�mites
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Asignar el Mesh al MeshFilter y MeshCollider
        MeshFilter meshFilter = newColliderObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshCollider meshCollider = newColliderObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        // Opcional: Renderizar el wireframe
        if (showWireframe)
        {
            MeshRenderer renderer = newColliderObject.AddComponent<MeshRenderer>();
            renderer.material = wireFrimeMaterial;
        }

        // Limpieza
        receivedVertices.Clear();
        receivedTriangles.Clear();
        fragmentsReceived = 0;
        totalExpectedFragments = 0;

        Debug.Log($"Mesh ensamblado correctamente con {mesh.vertexCount} v�rtices y {mesh.triangles.Length / 3} tri�ngulos.");
    }



    [PunRPC]
    public void EnviarMeshFinalizado_RPC()
    {
        Debug.Log("Mesh recibido y ensamblado correctamente.");
    }

    [PunRPC]
    public void AcuseReciboMeshCollider_RPC() { }
}
