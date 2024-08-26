using System.Threading.Tasks;
using UnityEngine;

public class ParticleHandler : MonoBehaviour
{
    [Header("Toggleables")]
    public bool gravity;
    public bool absolutePressure;
    public bool viscosity; // big performance boost if disabled
    public bool grid;

    [Space]

    [Header("Parameters")]
    public int particleAmount;
    public Gradient colorGradient;
    public Vector2 gradientVelocityRange;
    [Range(0, 0.5f)] public float gridSpacing;
    public Vector2 gridCenter;

    [Space]
    [Space]

    public Vector2 boundsRatio;
    public float boundsScale;
    private Vector2 boundsSize;
    public float mass = 1;

    [Space]
    [Space]

    [Range(0, 1)] public float particleRadius;
    [Range(0, 5)] public float smoothingRadius; 
    private SpatialGrid spatialGrid;

    [Space]
    [Space]

    public float targetDensity;
    public float pressureMultiplier;
    public float viscosityStrength;
    
    [Space]

    public float gravityForce;
    [Range(0, 1)] public float collisionDamping;

    [Space]
    [Space]

    public float interactRadius;
    public float interactStrength;

    [Space]

    [Header("References")]
    public GameObject particlePrefab;
    public Transform boundsVisualizer;

    [Space]

    [Header("Debug")]
    [SerializeField] private Vector2[] velocities;
    [SerializeField] private Vector2[] positions; 
    [SerializeField] private Vector2[] predictedPositions;
    [SerializeField] private GameObject[] particles;
    [SerializeField] private float[] densities;

    [SerializeField] private Vector2 mousePosition;
    [SerializeField] private int mousePressValue;

    [Space]

    [SerializeField] private bool isPlaying = false;



    private void Start()
    {
        velocities = new Vector2[particleAmount];
        positions = new Vector2[particleAmount];
        predictedPositions = new Vector2[particleAmount];
        particles = new GameObject[particleAmount];
        densities = new float[particleAmount];
        
        boundsSize = boundsRatio * boundsScale;
        boundsVisualizer.position = Vector3.zero;
        boundsVisualizer.localScale = (Vector3) boundsSize;

        spatialGrid = new SpatialGrid(smoothingRadius);
        GenerateParticleGrid();
    }


    private void GenerateParticleGrid() 
    {
        float spacing = gridSpacing + particleRadius;
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(particleAmount));
        int index = 0;

        for (int i = 0; i < gridSize; i++) 
        {
            for (int j = 0; j < gridSize; j++) 
            {
                if (index >= particleAmount) return;

                Vector2 position;

                if (grid) {
                    position = gridCenter + spacing * new Vector2(j, i) - ((gridSize-1) * spacing/2 * Vector2.one);
                } else {
                    position = new Vector2(
                        Random.Range(-boundsSize.x, boundsSize.x)/2, 
                        Random.Range(-boundsSize.y, boundsSize.y)/2
                    );
                }

                positions[index] = position;

                GameObject particle = Instantiate(particlePrefab, position, Quaternion.identity);
                particle.transform.localScale = Vector3.one * particleRadius;
                particle.name = "Particle " + index;
                particles[index] = particle;

                index++;
            }
        }
    }


    private void SimulationStep(float deltaTime)
    {

        // Mouse forces
        if (mousePosition != Vector2.negativeInfinity) {
            Parallel.For(0, particleAmount, i => {
                velocities[i] += InteractionForce(i) * deltaTime;
            });
        }

        // gravityForce & predicting next positions
        Parallel.For(0, particleAmount, i => {
            if (gravity) velocities[i] += gravityForce * deltaTime * Vector2.down;
            predictedPositions[i] = positions[i] + velocities[i] * deltaTime;
        });

        // Assign particles to spatial grid
        for(int i = 0; i < particleAmount; i++) {
            spatialGrid.AddParticle(i, predictedPositions[i]);
        };
        

        // Calculate & cache densities
        Parallel.For(0, particleAmount, i => {
            densities[i] = CalculateDensity(i);
        });

        // Pressure forces
        Parallel.For(0, particleAmount, i => {
            Vector2 pressureForce = CalculatePressureForce(i);
            Vector2 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += pressureAcceleration * deltaTime;
        });

        // Viscosity forces
        if (viscosity) {
            Parallel.For(0, particleAmount, i => {
                Vector2 viscosityForce = CalculatedViscosityForce(i);
                Vector2 viscosityAcceleration = viscosityForce / densities[i];
                velocities[i] += viscosityAcceleration * deltaTime;
            });
        }

        // Positions & collisions
        Parallel.For(0, particleAmount, i => {
            if (float.IsNaN(velocities[i].x)) {
                velocities[i].x = 0f;
                Debug.LogWarning("Particle " + i + " had a NaN velocity.x!");
            } 
            if (float.IsNaN(velocities[i].y)) {
                velocities[i].y = 0f;
                Debug.LogWarning("Particle " + i + " had a NaN velocity.y!");
            }
            positions[i] += velocities[i] * deltaTime;
            SimulateCollisions(ref positions[i], ref velocities[i]);
        });

        spatialGrid.Clear();
    }

    private void Update()
    {
        mousePressValue = Input.GetKey(KeyCode.Mouse0) ? -1 : Input.GetKey(KeyCode.Mouse1) ? 1 : 0;
        mousePosition = mousePressValue != 0 ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector2.negativeInfinity;

        if (Input.GetKeyDown(KeyCode.Space)) {
            isPlaying = !isPlaying;
            Debug.Log("Playing: " + isPlaying);
        }
    }

    private void FixedUpdate()  
    {
        if (!isPlaying) return;

        SimulationStep(Time.deltaTime);

        for (int i = 0; i < particleAmount; i++) {
            particles[i].transform.position = (Vector3) positions[i];
            particles[i].GetComponent<SpriteRenderer>()
                .color = colorGradient.Evaluate(MathUtils.ConvertRange(
                    Mathf.Clamp(velocities[i].magnitude, gradientVelocityRange.x, gradientVelocityRange.y),
                    gradientVelocityRange.x, gradientVelocityRange.y,
                    0f, 1f
                    ));
        }
    }


    private Vector2 InteractionForce(int particleIndex)
    {
        Vector2 interactionForce = Vector2.zero;
        Vector2 offset = mousePosition - positions[particleIndex];
        float sqrDist = Vector2.Dot(offset, offset);

        if (sqrDist < interactRadius * interactRadius)
        {
            float dist = Mathf.Sqrt(sqrDist);
            Vector2 dirToInputPoint = dist <= float.Epsilon ? Vector2.zero : offset / dist;
            float centerT = 1 - dist / interactRadius;
            interactionForce += (mousePressValue * interactStrength * dirToInputPoint - velocities[particleIndex]) * centerT;
        }

        return interactionForce;
    }


    private void SimulateCollisions(ref Vector2 position, ref Vector2 velocity)
    {
        Vector2 halfBoundsSize = (boundsSize - Vector2.one * particleRadius) / 2;

        if (Mathf.Abs(position.x) > halfBoundsSize.x)
        {
            position.x = halfBoundsSize.x * Mathf.Sign(position.x);
            velocity.x *= -1 * collisionDamping;
        }
        if (Mathf.Abs(position.y) > halfBoundsSize.y)
        {
            position.y = halfBoundsSize.y * Mathf.Sign(position.y);
            velocity.y *= -1 * collisionDamping;
        }
    }




    private float CalculateDensity(int particleIndex) 
    {
        float density = mass;

        foreach(int otherIndex in spatialGrid.GetNearbyParticleIndices(predictedPositions[particleIndex]))
        {
            if (otherIndex == particleIndex) {
                continue;
            }

            float dist = Vector2.Distance(predictedPositions[particleIndex], predictedPositions[otherIndex]);
            if (dist >= smoothingRadius) continue;

            float influence = MathUtils.SmoothingKernel(smoothingRadius, dist);
            density += influence * mass;
        }

        return density;
    }

    private Vector2 CalculatePressureForce(int particleIndex)
    {
        Vector2 pressureForce = Vector2.zero;

        foreach(int otherIndex in spatialGrid.GetNearbyParticleIndices(predictedPositions[particleIndex]))
        {
            if (otherIndex == particleIndex) continue;

            float dist = Vector2.Distance(predictedPositions[otherIndex], predictedPositions[particleIndex]);
            if (dist >= smoothingRadius) continue;
            Vector2 dir = dist == 0 ? GetRandomDirection() : (predictedPositions[otherIndex] - predictedPositions[particleIndex]).normalized;

            float slope = MathUtils.SmoothingKernalDerivative(smoothingRadius, dist);
            float density = densities[otherIndex];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            pressureForce += slope * mass / density * sharedPressure * dir;
        }

        return pressureForce;
    }

    private float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    private float ConvertDensityToPressure(float density)
    {
        float error = density - targetDensity;
        float pressure = error * pressureMultiplier;
        if (absolutePressure) pressure = Mathf.Max(0, pressure);
        return pressure;
    }

    private Vector2 CalculatedViscosityForce(int particleIndex)
    {
        Vector2 viscosityForce = Vector2.zero;
        Vector2 position = positions[particleIndex];

        foreach(int otherIndex in spatialGrid.GetNearbyParticleIndices(position))
        {
            if (otherIndex == particleIndex) continue;

            float dist = (position - positions[otherIndex]).magnitude;
            if (dist >= smoothingRadius) continue;
            float influence = MathUtils.ViscositySmoothingKernal(smoothingRadius, dist);
            viscosityForce += (velocities[otherIndex] - velocities[particleIndex]) * influence;
        }

        return viscosityForce * viscosityStrength;
    }

    private Vector2 GetRandomDirection()
    {
        System.Random random = new System.Random();
        Vector2 direction = new Vector2(
            (float) random.NextDouble() * 2 - 1, 
            (float) random.NextDouble() * 2 - 1
            ).normalized;
        return direction;
    }
}
