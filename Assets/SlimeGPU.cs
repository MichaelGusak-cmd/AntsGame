using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Agent
{
    public Vector2 position; 
    public float angle; //radians? idk
    public int species;
    public int caste;
    public int emit;
    public Vector3 smell;
    public Vector3 reaction;
    public Vector3 colour;
}

public struct Queen
{
    public Vector2 position;
    public float angle;
    public float radius;
    public int emit;
    public Vector3 colour;
}

public struct Pixel
{
    public Vector2 position;
    //values can be stored here as Vector3s,
    //Vector3 Carbon (Ground, Land, Sky)/(Ground, Water, Sky)
    //Vector3 Water (Ground, Land, Sky) etc
    public int value;
    public float strength;
    //colour?
}

public class SlimeGPU : MonoBehaviour
{
    private int pixelSizeInBytes = sizeof(float)*3  + sizeof(int)*1;
    private int agentSizeInBytes = sizeof(float)*12 + sizeof(int)*3; 

    public ComputeShader computeShader;

    public RenderTexture renderTexture;
    public RenderTexture trailMap;
    private Texture2D texture;

    public float zoom = 1.5f;

    public int numAgents = 1000; //multiple of 16 (cuz compute.Update is [16,1,1] threads)

    //Default settings seem to look nice
    public float moveSpeed = 50.0f;
    public float turnSpeed = 30.0f;

    public float sensorAngleDegrees = 5.0f;
    public float sensorOffsetDist = 20.0f;
    public float sensorAngleSpacing = 0.5f;
    public int sensorSize = 1; //maybe 3?

    public float evaporateSpeed = 0.5f;
    public float diffuseSpeed = 3.0f;
    public float mouseSpeed = 5.0f;

    public int stepsPerFrame = 1;
    public int width = 1280; //multiple of 8 (cuz compute.ProcessTrailMap is [8,8,1] threads)
    public int height = 720; //multiple of 8 (cuz compute.ProcessTrailMap is [8,8,1] threads)

    public float queenRadius = 5.0f;
    public bool queenPOV = false;
    private Queen queen;


    //Constants
    private static readonly int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 27 };
    private static readonly int bitDepth = 24; //24 or 32, for RenderTexture

    //Objects Lists and their buffers
    private Agent[] agents;
    private ComputeBuffer agentsBuffer;

    private Pixel[] pixels;
    private ComputeBuffer pixelsBuffer;

    //init bools
    private bool ready = false;
    private bool executed = false;

    //kernel indices
    private int updateIndex;
    private int processIndex;
    private int mouseIndex;
    private int initIndex;

    //mouse interaction vars:
    private Vector2 mousePos;
    private bool mousePressed = false;

    public void QueenSetup()
    {
        queen.position = new Vector2(width / 2.0f, height / 2.0f);
        queen.angle = 0.0f;
        queen.colour = new Vector3(0.2f, 0.8f, 0.9f);
        queen.emit = 5;
        queen.radius = queenRadius;
    }

    public void AgentsSetup()
    {
        agents = new Agent[numAgents];
        for (int i = 0; i < numAgents; i++)
        {
            CreateAgent(i);
        }
    }

    public void PixelsSetup()
    {
        pixels = new Pixel[width * height];
        for (int i = 0; i < width*height; i++)
        {
            CreatePixel(i);
        }
    }

    private void CreatePixel(int index)
    {
        Pixel pixel = new Pixel();
        pixel.position = new Vector2(index % width, (int)(index / width));
        pixel.value = 0;
        pixel.strength = 0;

        pixels[index] = pixel;
    }

    private void CreateAgent(int index)
    {
        Agent agent = new Agent();
        agent.position = new Vector2(width / 2.0f, height / 2.0f);
        agent.angle = Random.Range(0.0f, 2 * Mathf.PI);

        if (Random.Range(0.0f, 1.0f) < 0.4f) //40% odds
        {
            agent.species = 2;
            agent.caste = 1;

            agent.smell = new Vector3(2, 3, 5); //prime factors
            agent.reaction = new Vector3(1.0f, -1.0f, -1.0f);
            agent.emit = 2; //a product of primes for multiple emits(ex: 3*5)
            agent.colour = new Vector3(1, 0, 0);
        }
        else //60% odds
        {
            if (Random.Range(0.0f, 1.0f) < 0.5f) //30% odds
            { 
                agent.species = 2;
                agent.caste = 1;

                agent.smell = new Vector3(2, 3, 5); //prime factors
                agent.reaction = new Vector3(-1.0f, 1.0f, -1.0f);
                agent.emit = 3; //a product of primes for multiple emits(ex: 3*5)
                agent.colour = new Vector3(0, 1, 0);
            }
            else //30% odds
            {
                agent.species = 3;
                agent.caste = 1;

                agent.smell = new Vector3(2, 3, 5); //prime factors
                agent.reaction = new Vector3(-1.0f, -1.0f, 1.0f);
                agent.emit = 5; //a product of primes for multiple emits(ex: 15=3*5)
                agent.colour = new Vector3(0.0f, 0.75f, 1.0f);
            }
        }
        agents[index] = agent;
    }

    private void BufferSetup()
    {
        //Vectors are collections of floats (Vector2 = 2 floats)
        agentsBuffer = new ComputeBuffer(agents.Length, agentSizeInBytes);
        agentsBuffer.SetData(agents);

        pixelsBuffer = new ComputeBuffer(pixels.Length, pixelSizeInBytes);
        pixelsBuffer.SetData(pixels);
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
        initIndex = computeShader.FindKernel("Init");
        updateIndex = computeShader.FindKernel("Update");
        processIndex = computeShader.FindKernel("ProcessTrailMap");
        mouseIndex = computeShader.FindKernel("MousePressed");

        computeShader.SetTexture(updateIndex, "InputTexture", texture);
        computeShader.SetTexture(processIndex, "InputTexture", texture);
        computeShader.SetTexture(mouseIndex, "InputTexture", texture);

        computeShader.SetTexture(initIndex, "TrailMap", trailMap);
        computeShader.SetTexture(updateIndex, "TrailMap", trailMap);
        computeShader.SetTexture(processIndex, "TrailMap", trailMap);
        computeShader.SetTexture(mouseIndex, "TrailMap", trailMap);

        //Constant per creation
        computeShader.SetInt("numAgents", numAgents);
        computeShader.SetBuffer(updateIndex, "agents", agentsBuffer);
        computeShader.SetVector("queenColour", queen.colour);
        computeShader.SetInt("queenEmit", queen.emit);

        computeShader.SetBuffer(updateIndex, "pixels", pixelsBuffer);
        computeShader.SetBuffer(processIndex, "pixels", pixelsBuffer);
        computeShader.SetBuffer(mouseIndex, "pixels", pixelsBuffer);

        computeShader.SetBool("mousePressed", false); //init

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
                QueenSetup();
                AgentsSetup();
                PixelsSetup();

                BufferSetup();
                ShaderSetup();

                initTexture();
                ready = true; //dont set it to false anywhere after this
            }
        }
        //Screen update event
        if (Event.current.type.Equals(EventType.Repaint) && executed)
        {
            //Draw renderTexture to screen, there might be a better solution somewhere
            if (queenPOV)
            {
                float w = (zoom * Screen.width);
                float h = (zoom * Screen.height);

                float xPos = ((Screen.width / 2.0f) - (w * queen.position.x) / width);
                float yPos = (((Screen.height / 2.0f) + (h * queen.position.y) / height) - Screen.height * zoom);

                Graphics.DrawTexture(new Rect(xPos, yPos, w, h), renderTexture);
            }
            else
            {
                Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
            }
        }
    }

    void initTexture()
    { 
        computeShader.Dispatch(initIndex, width / 8, height / 8, 1);
        Graphics.CopyTexture(trailMap, texture); //Updating the texture input
    }

    void FixedUpdate()
    {
        //simulation subdivisions (remember to reduce values if increasing steps)
        //more steps will result in more interactions at same framerate
        for (int i = 0; i < stepsPerFrame; i++)
        {
            RunQueen();
            RunSimulation();
        }
    }
    void RunQueen()
    {
        queen.radius = queenRadius;
        //Vector2 direction = (getMousePos() - queen.position).normalized;
        Vector2 direction = new Vector2(0.0f, 0.0f);
        if (queenPOV)
            direction = (getMousePos() - new Vector2(width/2.0f, height/2.0f)).normalized;
        else
            direction = (getMousePos() - queen.position).normalized;
        queen.position += direction * moveSpeed * Time.deltaTime;
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

            if (mousePressed)
            { //send mouse info on mouse click (once per click)
                computeShader.SetBool("mousePressed", true);
                computeShader.SetVector("mousePos", mousePos);
                computeShader.SetFloat("mouseTime", Time.time);
                computeShader.SetFloat("mouseSpeed", mouseSpeed);

                mousePressed = false;
            }

            //update queen information:
            computeShader.SetVector("queenPos", queen.position);
            computeShader.SetFloat("queenRadius", queen.radius);

            //Agents Update Compute Shader:
            // Input:  AgentsBuffer, Texture2D Texture
            // Output: RenderTexture TrailMap
                computeShader.Dispatch(updateIndex, agents.Length / 16, 1, 1);
            Graphics.CopyTexture(trailMap, texture); //Updating the texture input

            //Processing Compute Shader:
            // Input:  Texture2D Texture
            // Output: Texture2D TrailMap
                computeShader.Dispatch(processIndex, width / 8, height / 8, 1);
            //Graphics.CopyTexture(trailMap, texture); //Updating the texture input

            //Mouse Pressed Compute Shader:
            // Input:  Texture2D Texture
            // Output: Texture2D TrailMap
            //computeShader.Dispatch(mouseIndex, width / 8, height / 8, 1);

            //=== At the end of the function ===\\
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
        if (Input.GetMouseButtonDown(0) && !mousePressed && ready)
        {
            mousePressed = true;
            mousePos = getMousePos(); //Input.mousePosition;
        }

        if (Input.GetKeyDown("space"))
        {
            queenPOV = !queenPOV;
        }
    }

    Vector2 getMousePos()
    {
        Vector3 mouseAt = Input.mousePosition;
        return new Vector2((mouseAt.x * width) / Screen.width,
                           (mouseAt.y * height) / Screen.height);

    }

    void OnApplicationQuit()
    {
        agentsBuffer.Dispose();
        pixelsBuffer.Dispose();
    }
}
