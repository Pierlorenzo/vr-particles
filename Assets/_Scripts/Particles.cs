using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Particles : MonoBehaviour
{
    #region Private Fields

    private Vector3[] randPos;
    private GameObject[] particles;
    private List<List<int>> particlesRelation = new List<List<int>>();

    #endregion

    #region Public Fields

    [Header("Materials")]
    [Tooltip("The material to use for the lines")]
    public Material lineMaterial;
    [Tooltip("The material to use for the particles")]
    public Material particlesMaterial;

    [Header("Overlap Sphere settings (for particles connection lines generation)")]
    [Tooltip("The radius of the Overlap Sphere")]
    public float radius = 0.6f;
    [Tooltip("Layer where to check Overlap Shpere between particles")]
    public LayerMask mask;


    [Header("Particles settings")]
    [Tooltip("Total number of particles to be generated")]
    public int numberOfParticles = 50;
    [Tooltip("Scale of the particle")]
    public float particleScale = 0.008f;


    [Header("Movement Settings")]
    [Tooltip("Max distance before generating new rand position")]
    public float maxDistance = 1f;
    [Tooltip("Particles movement speed")]
    public float speed = 0.05f;

    // Using float instead of Vector2 for slightly better performance

    [Header("Particles movement area")]
    [Tooltip("Min x value used for random range")]
    public float xPosMin = -4;
    [Tooltip("Max x value used for random range")]
    public float xPosMax = 4;
    [Space(10)]
    [Tooltip("Min y value used for random range")]
    public float yPosMin = 0;
    [Tooltip("Max y value used for random range")]
    public float yPosMax = 3;
    [Space(10)]
    [Tooltip("Min z value used for random range")]
    public float zPosMin = 0;
    [Tooltip("Max z value used for random range")]
    public float zPosMax = 6;
    
    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        UpdateParticlesMovement();
    }

    private void OnPostRender()
    {        
        DrawConnectingLines();
    }

    #endregion

    #region Private Methods

    private void Init()
    {
        // Setup containers
        particles = new GameObject[numberOfParticles];
        randPos = new Vector3[numberOfParticles];
        GameObject allParticles = new GameObject();
        allParticles.name = "Particles";

        // Create and setup the particles
        for (int i = 0; i < numberOfParticles; i++)
        {
            particles[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particles[i].layer = LayerMask.NameToLayer("Particles");
            particles[i].name = i.ToString();
            particles[i].transform.SetParent(allParticles.transform);
            particles[i].transform.localScale = new Vector3(particleScale, particleScale, particleScale);
            particles[i].GetComponent<MeshRenderer>().material = particlesMaterial;
            Rigidbody rb = particles[i].AddComponent<Rigidbody>();
            rb.mass = 2;
            rb.drag = 0.5f;
            rb.useGravity = false;
            rb.isKinematic = true;

            // Spawn particle at a random position within the range
            randPos[i] = new Vector3(UnityEngine.Random.Range(xPosMin, xPosMax), UnityEngine.Random.Range(yPosMin, yPosMax), UnityEngine.Random.Range(zPosMin, zPosMax));
            particles[i].transform.position = randPos[i];

            // Setup particlesRelation list, used later to avoid to draw 2 times the same line.
            particlesRelation.Add(new List<int>());
        }

    }

    private void UpdateParticlesMovement()
    {
        for (int i = 0; i < numberOfParticles; i++)
        {
            // Lerp the particle position to the random position
            particles[i].transform.position = Vector3.Lerp(particles[i].transform.position, randPos[i], Time.deltaTime * speed);
            // When the particle get maxDistance close to the randomposition, generate a new one
            if (Vector3.Distance(particles[i].transform.position, randPos[i]) <= maxDistance)
                randPos[i] = new Vector3(UnityEngine.Random.Range(xPosMin, xPosMax), UnityEngine.Random.Range(yPosMin, yPosMax), UnityEngine.Random.Range(zPosMin, zPosMax));
        }
    }

    private void DrawConnectingLines()
    {

        for (int i = 0; i < numberOfParticles; i++)
        {
            Vector3 pointPos = particles[i].transform.position;
            Collider[] hitColliders = Physics.OverlapSphere(pointPos, radius, mask);
            if (hitColliders.Length > 3)
            {
                // Only consider 3 points to avoid too many line between points
                hitColliders = SubArray(hitColliders, 0, 3);
            }

            // Physics.OverlapSphere return also a collision with the particle itself. This is a workaround to get rid of it.
            for (int j = 0; j < hitColliders.Length; j++)
            {
                if (hitColliders[j].name == i.ToString())
                {
                    int index = Array.IndexOf(hitColliders, hitColliders[j]);
                    hitColliders = RemoveAt(hitColliders, index);
                    break;
                }
            }

            // Check if there is alrady a connection between particle 1 and particle 2. We want to avoid to draw the same line twice.
            // This workaround saves the relationship between particles in the particlesRelation list, and check if the relationship already exists.
            foreach (Collider item in hitColliders)
            {
                if (particlesRelation[Convert.ToInt32(item.name)].Contains(i))
                {
                    int index = Array.IndexOf(hitColliders, item);
                    hitColliders = RemoveAt(hitColliders, index);
                }
            }
            
            // Update the list for the next iteration check.
            List<int> intList = new List<int>();
            foreach (Collider item in hitColliders)
            {
                intList.Add(Convert.ToInt32(item.name));
            }

            particlesRelation[i] = intList;
            int counter = 0;
            foreach (int item in particlesRelation[i])
            {
                // Generate alpha value to apply to the material color such that: the furthest the distance between the two points, the lower the alpha value.
                float a = CalculateAlpha(pointPos, hitColliders[counter].transform.position);
                // Generate line to connect the two points and apply material.
                GL.Begin(GL.LINES);
                lineMaterial.SetPass(0);
                GL.Color(new Color(lineMaterial.color.r, lineMaterial.color.g, lineMaterial.color.b, a));
                GL.Vertex3(hitColliders[counter].transform.position.x, hitColliders[counter].transform.position.y, hitColliders[counter].transform.position.z);
                GL.Vertex3(pointPos.x, pointPos.y, pointPos.z);
                GL.End();
                counter++;
            }
        }
    }

    private float CalculateAlpha(Vector3 pos1, Vector3 pos2)
    {
        float dist = Vector3.Distance(pos1, pos2);
        return (1 - (dist / radius));
    }

    #endregion

    #region Helper Methods

    // Keep only lenght elements of a given array starting at index.
    private T[] SubArray<T>(T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
    
    // Remove element at a specific index of an array.
    public T[] RemoveAt<T>(T[] source, int index)
    {
        T[] dest = new T[source.Length - 1];
        if (index > 0)
            Array.Copy(source, 0, dest, 0, index);

        if (index < source.Length - 1)
            Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

        return dest;
    }

    #endregion
}
