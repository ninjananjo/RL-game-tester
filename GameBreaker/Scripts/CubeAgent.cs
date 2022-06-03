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

    private GameArea gameArea;
    new private Rigidbody rigidbody;
    private TrailRenderer trail;
    private GameObject goal;
    private GameObject sticky_1;
    private GameObject sticky_2;
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
        sticky_1 = GameObject.Find("StickyCube_01");
        sticky_2 = GameObject.Find("StickyCube_02");
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

        if (isMoving) {     
            rigidbody.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
            transform.Rotate(transform.up * turnAmount * turnSpeed * Time.fixedDeltaTime);                      
        }

        // Apply a tiny negative reward every step to encourage action
        MaxStep = 1000;
        if (MaxStep > 0) AddReward(-1f / MaxStep);

        // On time out update log
        if (GetCumulativeReward() < -0.999f) UpdateLog();

        // Control trail renderer. If agent is about to timeout or reach goal turn off trail renderer
        if (GetCumulativeReward() > -0.98f && GetCumulativeReward() < -0.001f && DistanceToObject(goal) > 1.5f && DistanceToObject(sticky_1) > 1.5f && DistanceToObject(sticky_2) > 1.5f) {
            trail.emitting = true;
        }
        else {            
            trail.emitting = false;
        }
    }
    
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
            // Format: Current Datetime, Episode, steps, reward, found stick cube 1, found stick cube 2, found secret wall 1, found secret wall 2
            logFile.WriteLine(logEntry);
        }

    }

    /// <summary>
    /// Read inputs from the keyboard and convert them to a list of actions.
    /// This is called only when the player wants to control the agent and has set
    /// Behavior Type to "Heuristic Only" in the Behavior Parameters inspector.
    /// </summary>
    /// <returns>A vectorAction array of floats that will be passed into <see cref="AgentAction(float[])"/></returns>
    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     int forwardAction = 0;
    //     int turnAction = 0;
    //     if (Input.GetKey(KeyCode.W))
    //     {
    //         // move forward
    //         forwardAction = 1;
    //     }
    //     if (Input.GetKey(KeyCode.A))
    //     {
    //         // turn left
    //         turnAction = 1;
    //     }
    //     else if (Input.GetKey(KeyCode.D))
    //     {
    //         // turn right
    //         turnAction = 2;
    //     }

    //     // Put the actions into the array
    //     actionsOut.DiscreteActions.Array[0] = forwardAction;
    //     actionsOut.DiscreteActions.Array[1] = turnAction;
    // }

    /// <summary>
    /// Randomly creates a list of actions.
    /// Behavior Type to "Heuristic Only" in the Behavior Parameters inspector.
    /// </summary>
    /// <returns>A vectorAction array of floats that will be passed into <see cref="AgentAction(float[])"/></returns>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int forwardAction = 0;
        int turnAction = 0;
        
        if (Random.Range(0, 2) == 0)
        {
            // move forward
            forwardAction = 1;
        }

        int randomTurnAction = Random.Range(0, 3);
        if (randomTurnAction == 1)
        {
            // turn left
            turnAction = 1;
        }
        else if (randomTurnAction == 2)
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
        sensor.AddObservation((goal.transform.position - transform.position).normalized);

        // Direction CubeAgent is facing (1 Vector3 = 3 values)
        sensor.AddObservation(transform.forward);

        // 1 + 3 + 3 = 7 total values
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

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name == "FakeBarrier_01")
        {
            foundSecretWall_1 = true;
        }

        if(other.gameObject.name == "FakeBarrier_02")
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

    public int GetTotalSteps()
    {
        return totalSteps + StepCount;
    }

    public void setIsMoving(bool state)
    {
        isMoving = state;
    }


}
