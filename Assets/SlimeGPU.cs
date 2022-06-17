using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Agent
{
    public Vector2 position; 
    public float angle; //radians? idk
}

public class SlimeGPU : MonoBehaviour
{
    public ComputeShader computeShader;

    public RenderTexture renderTexture;
    public RenderTexture trailMap;
    private Texture2D texture;

    public int numAgents = 1000;

    //Default settings seem to look nice
    public float moveSpeed = 50.0f;
    public float turnSpeed = 30.0f;

    public float sensorAngleDegrees = 5.0f;
    public float sensorOffsetDist = 20.0f;
    public float sensorAngleSpacing = 0.5f;
    public int sensorSize = 1; //maybe 3?

    public float evaporateSpeed = 0.5f;
    public float diffuseSpeed = 3.0f;
    public int stepsPerFrame = 1;
    public int width = 1280;
    public int height = 720;

    private int bitDepth = 24; //24 or 32, for RenderTexture

    //Objects Lists and their buffers
    private Agent[] agents;
    private ComputeBuffer agentsBuffer;

    //init bools
    private bool ready = false;
    private bool executed = false;

    //kernel indices
    private int updateIndex;
    private int processIndex;

    public void AgentsSetup()
    {
        agents = new Agent[numAgents];
        for (int i = 0; i < numAgents; i++)
        {
            CreateAgent(i);
        }
    }

    private void CreateAgent(int index)
    {
        Agent agent = new Agent();
        agent.position = new Vector2(width / 2.0f, height / 2.0f);
        agent.angle = Random.Range(0.0f, 2 * Mathf.PI);
        agents[index] = agent;
    }

    private void BufferSetup()
    {
        int size = sizeof(float) * 3; //vector2 = 2 floats, angle = float, total: 3 floats
        agentsBuffer = new ComputeBuffer(agents.Length, size);
        agentsBuffer.SetData(agents);
    }

    private void ShaderSetup()
    {
        //Creating Textures
        renderTexture = new RenderTexture(width, height, bitDepth);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();

        trailMap = new RenderTexture(width, height, bitDepth);
        trailMap.enableRandomWrite = true;
        trailMap.filterMode = FilterMode.Point;
        trailMap.Create();

        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

        //Setting the textures, only needs to be done once
        //Textures should be kept on the GPU for performance
        updateIndex = computeShader.FindKernel("Update");
        processIndex = computeShader.FindKernel("ProcessTrailMap");

        computeShader.SetTexture(updateIndex, "InputTexture", texture);
        computeShader.SetTexture(processIndex, "InputTexture", texture);

        computeShader.SetTexture(updateIndex, "TrailMap", trailMap);
        computeShader.SetTexture(processIndex, "TrailMap", trailMap);

        //Constant per creation
        computeShader.SetInt("numAgents", numAgents);
        computeShader.SetBuffer(updateIndex, "agents", agentsBuffer);

        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
    }

    private void OnGUI()
    {
        //Create button, not necessary but good enough for a temp 'start' button
        if (GUI.Button(new Rect(0, 0, 100, 50), "Create"))
        {
            if (!ready)
            {
                AgentsSetup();
                BufferSetup();
                ShaderSetup();

                ready = true; //dont set it to false anywhere after this
            }
        }
        //Screen update event
        if (Event.current.type.Equals(EventType.Repaint) && executed)
        {
            //Draw renderTexture to screen, there might be a better solution somewhere
            Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
        }

        if (executed)
        {
            //runs after first execution of RunSimulation()
        }
    }

    void FixedUpdate()
    {
        //simulation subdivisions (remember to reduce values if increasing steps)
        //more steps will result in more accurate interactions
        for (int i = 0; i < stepsPerFrame; i++)
        {
            RunSimulation();
        }
    }

    void RunSimulation()
    {
        if (ready) //will only run after init conditions complete
        {
            //Setting the modifiable variables
            computeShader.SetFloat("deltaTime", Time.deltaTime);
            computeShader.SetFloat("time", Time.time);

            computeShader.SetFloat("moveSpeed", moveSpeed);
            computeShader.SetFloat("turnSpeed", turnSpeed);

            computeShader.SetFloat("sensorAngleDegrees", sensorAngleDegrees);
            computeShader.SetFloat("sensorOffsetDist", sensorOffsetDist);
            computeShader.SetFloat("sensorAngleSpacing", sensorAngleSpacing);
            computeShader.SetInt("sensorSize", sensorSize);

            computeShader.SetFloat("diffuseSpeed", diffuseSpeed);
            computeShader.SetFloat("evaporateSpeed", evaporateSpeed);

            //Agents Update Compute Shader:
            // Input:  AgentsBuffer, Texture2D Texture
            // Output: RenderTexture TrailMap
            computeShader.Dispatch(updateIndex, agents.Length / 16, 1, 1);

            //Updating the texture input:
            Graphics.CopyTexture(trailMap, texture);

            //Processing Compute Shader:
            // Input:  Texture2D Texture
            // Output: Texture2D TrailMap
            computeShader.Dispatch(processIndex, width / 8, height / 8, 1);

            //Setting trailmap to renderTexture (to prevent tearing)
            Graphics.Blit(trailMap, renderTexture);

            //only important for first loop, so renderer knows it can start drawing
            executed = true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
    }



    // Update is called once per frame
    void Update()
    {
    }

    void OnApplicationQuit()
    {
        agentsBuffer.Dispose();
    }
}
