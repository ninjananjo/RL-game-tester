using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Core;

public class GameArea : MonoBehaviour
{
    [Tooltip("The agent inside the area")]
    public CubeAgent cubeAgent;
    [Tooltip("The goal inside the area")]
    public GameObject goal;
    [Tooltip("The TextMeshPro text that shows the current reward and episode")]
    public TextMeshPro scoreBoard;    
    [Tooltip("The TextMeshPro text that shows the high score")]
    public TextMeshPro highScoreBoard;
    [Tooltip("The TextMeshPro text that shows the agent's total steps")]
    public TextMeshPro stepsBoard;
    
    private Vector3 agentPosition;
    // private int agentStopCount = 0;
    

    /// <summary>
    /// Reset the area, including agent and goal placement
    /// </summary>
    public void ResetArea()
    {        
        PlaceAgent();
        PlaceGoal();
        cubeAgent.setIsMoving(true);
    }


    /// <summary>
    /// Choose a random position on the X-Z plane within a partial donut shape
    /// </summary>
    /// <param name="center">The center of the donut</param>
    /// <param name="minAngle">Minimum angle of the wedge</param>
    /// <param name="maxAngle">Maximum angle of the wedge</param>
    /// <param name="minRadius">Minimum distance from the center</param>
    /// <param name="maxRadius">Maximum distance from the center</param>
    /// <returns>A position falling within the specified region</returns>
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius)
    {
        float radius = minRadius;
        float angle = minAngle;

        if (maxRadius > minRadius)
        {
            // Pick a random radius
            radius = UnityEngine.Random.Range(minRadius, maxRadius);
        }

        if (maxAngle > minAngle)
        {
            // Pick a random angle
            angle = UnityEngine.Random.Range(minAngle, maxAngle);
        }

        // Center position + forward vector rotated around the Y axis by "angle" degrees, multiplies by "radius"
        return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    /// <summary>
    /// Place the agent in the area
    /// </summary>
    private void PlaceAgent()
    {
        Rigidbody rigidbody = cubeAgent.GetComponent<Rigidbody>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        cubeAgent.transform.position = ChooseRandomPosition(transform.position, 150f, 210f, 10f, 15f) + Vector3.up * .5f;
        cubeAgent.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        agentPosition = cubeAgent.transform.position;        
    }

    /// <summary>
    /// Place the goal in the area
    /// </summary>
    private void PlaceGoal()
    {
        Rigidbody rigidbody = goal.GetComponent<Rigidbody>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        goal.transform.position = ChooseRandomPosition(transform.position, -30f, 30f, 8f, 12f) + Vector3.up * 1f;
        goal.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    /// <summary>
    /// Called when the game starts
    /// </summary>
    private void Start()
    {
        Application.targetFrameRate = 60;       
        ResetArea();
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    private void Update()
    {
        // check every 360 frames if agent has moved
        // if(Time.frameCount % 360 == 0) IsAgentStuck(cubeAgent.transform.position);
        
        // Update the HUD
        scoreBoard.text = 
            "Current\nEpisode: " + cubeAgent.CompletedEpisodes.ToString()
            + "\nReward: " + cubeAgent.GetCumulativeReward().ToString("0.00");
        highScoreBoard.text = 
            "High Score\nEpisode: " + cubeAgent.GetBestEpiside().ToString()
            + "\nReward: " + cubeAgent.GetHighScore().ToString("0.00");
        stepsBoard.text = 
            "Total Steps: " + cubeAgent.GetTotalSteps().ToString("#,0");
    }


    /// <summary>
    /// Check if agent has moved from last position
    /// </summary>
    // private void IsAgentStuck(Vector3 agentNewPosition)
    // {
    //     if(Vector3.Distance(agentPosition, agentNewPosition) < 0.1)
    //     {
    //         agentStopCount++;
    //         Debug.Log("Agent not moving");
    //     }
    //     agentPosition = agentNewPosition;        
    // }

}
