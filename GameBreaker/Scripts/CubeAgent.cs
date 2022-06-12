using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.SceneManagement;

public class CubeAgent : Agent
{
    [Tooltip("How fast the agent moves forward")]
    public float moveSpeed = 5f;
    [Tooltip("How fast the agent turns")]
    public float turnSpeed = 180f;
    [Tooltip("Set level the agent is in")]
    public int level = 2;

    private GameArea gameArea;
    new private Rigidbody rigidbody;
    private TrailRenderer trail;
    private GameObject goal;
    private GameObject sticky_1;
    private GameObject sticky_2;
    private GameObject fakeBarrier_1;
    private GameObject fakeBarrier_2;
    private bool isMoving = true;
    private float highScore = 0f;
    private int bestEpisode = 0;
    private int totalSteps = 0;
    private bool foundSticky_1 = false;
    private bool foundSticky_2 = false;
    private bool foundSecretWall_1 = false;
    private bool foundSecretWall_2 = false;
    private string filename = "/gamelog.txt";
    

    /// <summary>
    /// Initial setup, called when the agent is enabled
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        gameArea = GetComponentInParent<GameArea>();
        goal = gameArea.goal;        
        sticky_1 = GameObject.Find("/Level_" + level + "/StickyCube_01");
        sticky_2 = GameObject.Find("/Level_" + level + "/StickyCube_02");
        fakeBarrier_1 = GameObject.Find("/Level_" + level + "/FakeBarrier_01");
        fakeBarrier_2 = GameObject.Find("/Level_" + level + "/FakeBarrier_02");
        rigidbody = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// Perform actions based on a vector of numbers
    /// </summary>
    /// <param name="actionBuffers">The struct of actions to take</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Convert the first action to forward movement
        float forwardAmount = actionBuffers.DiscreteActions[0];

        // Convert the second action to turning left or right
        float turnAmount = 0f;
        if (actionBuffers.DiscreteActions[1] == 1f)
        {
            turnAmount = -1f;
        }
        else if (actionBuffers.DiscreteActions[1] == 2f)
        {
            turnAmount = 1f;
        }

        // Apply movement to agent unless disabled
        if (isMoving) {     
            rigidbody.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
            transform.Rotate(transform.up * turnAmount * turnSpeed * Time.fixedDeltaTime);                      
        }

        // Apply a tiny negative reward every step to encourage action
        if (MaxStep > 0) AddReward(-1f / MaxStep);

        // On time out update log
        if (StepCount == MaxStep) UpdateLog();    

        // Control trail renderer. If agent is about to timeout or reach goal turn off trail renderer
        // if (GetCumulativeReward() > -0.98f && GetCumulativeReward() < -0.005f && DistanceToObject(goal) > 2.0f && DistanceToObject(sticky_1) > 2.0f && DistanceToObject(sticky_2) > 2.0f) {
        if (StepCount > 1 && StepCount < MaxStep-1 && DistanceToObject(goal) > 2.0f && DistanceToObject(sticky_1) > 2.0f && DistanceToObject(sticky_2) > 2.0f) {
            trail.emitting = true;
        }
        else {            
            trail.emitting = false;
        }
    }
    
    /// <summary>
    /// Write episode stats to log file.
    /// </summary>
    private void UpdateLog()
    {
        string logEntry = System.DateTime.Now 
            + "," + CompletedEpisodes 
            + "," + StepCount 
            + "," + GetCumulativeReward()
            + "," + foundSticky_1
            + "," + foundSticky_2
            + "," + foundSecretWall_1
            + "," + foundSecretWall_2;
        // Debug.Log(logEntry);        
        totalSteps += StepCount;
        
        using(System.IO.StreamWriter logFile = new System.IO.StreamWriter(Application.dataPath + "/../results" + filename, append: true))
        {
            logFile.WriteLine(logEntry);
        }

    }

    /// <summary>
    /// Read inputs from the keyboard and convert them to a list of actions.
    /// This is called only when the player wants to control the agent and has set
    /// Behavior Type to "Heuristic Only" in the Behavior Parameters inspector.
    /// </summary>
    /// <returns>A vectorAction array of floats that will be passed into <see cref="AgentAction(float[])"/></returns>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int forwardAction = 0;
        int turnAction = 0;
        if (Input.GetKey(KeyCode.W))
        {
            // move forward
            forwardAction = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            // turn left
            turnAction = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            // turn right
            turnAction = 2;
        }

        // Put the actions into the array
        actionsOut.DiscreteActions.Array[0] = forwardAction;
        actionsOut.DiscreteActions.Array[1] = turnAction;
    }

    /// <summary>
    /// When a new episode begins, reset the agent and area
    /// </summary>
    public override void OnEpisodeBegin()
    {
        foundSticky_1 = false;
        foundSticky_2 = false;
        foundSecretWall_1 = false;
        foundSecretWall_2 = false;
        gameArea.ResetArea();        
    }

    /// <summary>
    /// Collect all non-Raycast observations
    /// </summary>
    /// <param name="sensor">The vector sensor to add observations to</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Distance to the goal (1 float = 1 value)
        sensor.AddObservation(DistanceToObject(goal));

        // Direction to goal (1 Vector3 = 3 values)
        // sensor.AddObservation((goal.transform.position - transform.position).normalized);

        // Direction CubeAgent is facing (1 Vector3 = 3 values)
        // sensor.AddObservation(transform.forward);

        // Target and Agent positions (2 Vector3 = 6 values)
        sensor.AddObservation(goal.transform.localPosition.normalized);
        sensor.AddObservation(transform.localPosition.normalized);

        // Target and Agent x & z positions (2* x & z = 4 values)
        // sensor.AddObservation(goal.transform.localPosition.x);
        // sensor.AddObservation(goal.transform.localPosition.z);
        // sensor.AddObservation(transform.localPosition.x);
        // sensor.AddObservation(transform.localPosition.z);
    }

    /// <summary>
    /// Calculate the distance between a game object and the agent.
    /// </summary>
    /// <param name="obj">The gameobject</param>
    // <returns>The distance between the gameobjects</returns>
    private float DistanceToObject(GameObject obj)
    {
        return Vector3.Distance(obj.transform.position, transform.position);
    }

    /// <summary>
    /// When the agent collides with something, take action
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("goal"))
        {
            AddReward(1f);
            if(GetCumulativeReward() > highScore) {
                highScore = GetCumulativeReward();
                bestEpisode = CompletedEpisodes;
            }
            isMoving = false;
            UpdateLog();          
            EndEpisode();
        }

        if (collision.gameObject == sticky_1)
        {
            isMoving = false;
            foundSticky_1 = true;
        }

        if (collision.gameObject == sticky_2)
        {
            isMoving = false;
            foundSticky_2 = true;
        }

    }

    /// <summary>
    /// When the agent enters a trigger, take action
    /// </summary>
    /// <param name="other">The triggr info</param>
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == fakeBarrier_1)
        {
            foundSecretWall_1 = true;
        }

        if(other.gameObject == fakeBarrier_2)
        {
            foundSecretWall_2 = true;
        }
    }


    /// <summary>
    /// Retrieves the highest episode reward for the Agent.
    /// </summary>
    /// <returns>The highest episode reward.</returns>
    public float GetHighScore()
    {
        return highScore;
    }

    /// <summary>
    /// Retrieves the episode with the highest reward.
    /// </summary>
    /// <returns>The episode with highest reward.</returns>
    public int GetBestEpiside()
    {
        return bestEpisode;
    }

    /// <summary>
    /// Retrieves the step count from prior episodes and current episodes.
    /// </summary>
    /// <returns>The agents total steps</returns>
    public int GetTotalSteps()
    {
        return totalSteps + StepCount;
    }

    /// <summary>
    /// Enable or disable agent's movement.
    /// </summary>
    /// <param name="state">Can agent move</param>
    public void setIsMoving(bool state)
    {
        isMoving = state;
    }


}
