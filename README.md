# Context Steering

This is a Unity package to add context steering to a wide range of projects quickly and easily.  
The inspiration for this project was due to a [Devlog](https://www.youtube.com/watch?v=6BrZryMz-ac) by [Game Endeavor](https://www.youtube.com/channel/UCLweX1UtQjRjj7rs_0XQ2Eg) and a subsequent read of the companion article [**Context Steering** Behavior-Driven Steering at the Macro Scale](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf) by Andrew Fray.

# Contents
- [What is context steering?](#what-is-context-steering-)
- [Getting Started](#getting-started)
  * [Check out the Demo scene](#check-out-the-demo-scene)
  * [Basic usage](#basic-usage)
    + [Controllers](#controllers)
    + [Behaviours](#behaviours)
    + [Masks](#masks)
  * [Installation](#installation)

<small><i><a href='http://ecotrust-canada.github.io/markdown-toc/'>Table of contents generated with markdown-toc</a></i></small>


# What is context steering?

**It is not a replacement for pathfinding.** However it is relatively easy to use it in conjunction with a pathfinding system (like the Unity NavMesh) - an example of this is included in the Demo scene.

Context steering involves defining multiple behaviours on an entity (aka. GameObject), combining them, and producing a single output vector that represents a direction that is statistically good for the entity to move in. This allows us to define many discrete simplistic behaviours that when combined can produce seemingly complex movement.

## Introduction
Steering behaviors are movement algorithms that determine where an AI agent should be next.
These algorithms use basic information about the AI agent (current position, velocity, direction, ...) and the world to make a decision on where to go next.
The steering behavior will calculate a direction vector to adjust the movement of the AI agent.

These simple steeringbehaviors can then be combined in combined steering behaviors the create more complex movement and make the agent seem more intelligent.

## The need for context steering
The need for context steering arises when players are able to inspect individual AI agents and observe them closely avoiding collision with other agents and with the static world. The more steering behaviors are combined the harder it will become for the developer to tune the parameters of each and every one of those steering behaviors to achieve the behavior needed. This could also possibly mean that the behavior components itself will grow in size and become tightly coupled. These tightly coupled behaviors can then cause problems in terms of maintainability of the codebase.

Context steering combines small context behaviors that can be combinded together without tightly coupling them.
## Context steering overview
Think of context steering as a steering behavior that wants to go in a certain amount of directions equally divided over a circle.
If it was just this, the agent would stand still because all directions have an equal length thus they all apply the same amount of force to the agent negating eachother. We alter the force applied by these directions by the desire of the behavior to go in a certain direction. 
These scalar values of how desired or undesired a certain direction is are stored in context maps.
![Directions](https://user-images.githubusercontent.com/41028126/151255179-698c4187-62a6-4f96-9a4c-20d895df3d42.png)

image from: Game AI 2 Chapter 18: Behavior-Driven steering at the macro scale
### Context maps
Each context behavior has 2 context maps, an interest context map and a danger context map. The context steeringbehavior uses the interst map to represent its desire to go into a certain direction while the danger map represents the oposite.

For example a chase or seek context behavior will fill the slots of the interest map with higher scaler values relative the amount the corresponding direction of the slot is pointing in the same direction as the direction vector to the target of the chase behavior (think Dot product).

An avoid context steering behavior will do the exact opposite this behavior.
Each slot of the danger map again corresponds to a direction the agent can move in and the value in the slot itself represents the behaviors desire to NOT go into that direction.

Keep in mind that there should always be an equal amount of slots in both the interest map and danger map as there are directions the agent can move in.

![context map](https://user-images.githubusercontent.com/41028126/151255261-b1766052-473b-4a7b-b0f2-edc07f100a65.png)

image from: Game AI 2 Chapter 18: Behavior-Driven steering at the macro scale
### Context Merger
The context merger will gather all interest and danger maps from all context behaviors active on the AI agent and merge them together to get to a final direction vector result to move the agent with.

#### How the context maps are merged

First all context maps are gathered by the context merger to build a final interest and danger map to calculate the final direction.

For both the interest and the danger map, the merger loops over all slots and picks the highest value it can find for that particular slot from all its corresponding maps (interest maps for final interest map, danger maps for final danger map). We could also calculate the average to have for example even less of a desire to move to a spot where 2 avoid targets are but this is unneccesary because the avoidance of the first obstacle will already keep us safe from the obstacle behind it.

When we then have calculated both the final interest and danger map we subtract each interest slot of our final interest map by its corresponding slot in the final danger map. This way the interests towards our goal are altered if there is and obstacle on our path.
![parsing context map](https://user-images.githubusercontent.com/41028126/151255362-ae85c3dd-fd2c-4733-8b7a-e6e608562433.png)

image from: Game AI 2 Chapter 18: Behavior-Driven steering at the macro scale
## Implementation
This section will describe the implemetion of context steering in the unity application.
### Context Merger
#### Memeber variables
- m_MapResolution: Determines the amount of directions used for calculating the final direction, also determines size of the interest and danger maps
- m_MovementSpeed: Movement speed of the agent
- m_Behaviors: Array of all Context behaviors associated to this agent
- m_Directions: list of direction vectors
- m_InterestMap: final interest map
- m_DangerMap: final danger map
````
    [SerializeField] private int m_MapResolution;
    [SerializeField] private float m_MovementSpeed;

    [SerializeField] private BaseContextBehavior[] m_Behaviors;

    private List<Vector2> m_Directions;
    private List<float> m_InterestMap;
    private List<float> m_DangerMap;
    Rigidbody2D m_Rigibody2D;
````

#### Initializing the directions
This function initializes all directions equally divided on a circle these are the directions that will be altered by the desires from the context maps
````
    void InitializeDirections()
    {
        float twoPi = Mathf.PI * 2;
        float directionInterval = twoPi / m_MapResolution;
        for (int i = 0; i < m_MapResolution; i++)
        {
            float currentAngle = i * directionInterval;

            m_Directions.Add(new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)));

        }

        m_InterestMap = new List<float>(new float[m_Directions.Count]);
        m_DangerMap = new List<float>(new float[m_Directions.Count]);

    }
````



#### Merging maps
First we gather all interest maps from all behaviors.
````
       List<List<float>> interestMaps = new List<List<float>>();
        List<List<float>> dangerMaps = new List<List<float>>();

        foreach(BaseContextBehavior behavior in m_Behaviors)
        {
            interestMaps.Add(behavior.GetInterestMap(gameObject.transform.position, ref m_Directions));
            dangerMaps.Add(behavior.GetDangerMap(gameObject.transform.position, ref m_Directions));
        }
````
Then we calculate the biggest value for a slot ranging all gathered interest maps. We do this for each direction (for each slot in the interest map)
````
        for (int i = 0; i < m_InterestMap.Count; i++)
        {
            float biggestInterestForThisSlot = 0;
            for (int k = 0; k < interestMaps.Count; k++)
            {
                if (interestMaps[k][i] > biggestInterestForThisSlot)
                    biggestInterestForThisSlot = interestMaps[k][i];
            }
            
            m_InterestMap[i] = biggestInterestForThisSlot;
        }
````
Then we do the exact same for all the danger maps.
````
        for (int i = 0; i < m_DangerMap.Count; i++)
        {
            float biggestInterestForThisSlot = 0;
            for (int k = 0; k < dangerMaps.Count; k++)
            {
                if (dangerMaps[k][i] > biggestInterestForThisSlot)
                    biggestInterestForThisSlot = dangerMaps[k][i];
            }

            m_DangerMap[i] = biggestInterestForThisSlot;
        }
````

#### Calculating final interest map
We calculate the final interest map by subtracting our current interest map by the values of our danger map.
````
        for (int i = 0; i < m_DangerMap.Count; i++)
        {
            m_InterestMap[i] -= m_DangerMap[i];
        }
````
Then finally we search for the biggest desire in our interest map and use that direction as a movement direction.
````
        float biggestInterest = Mathf.Max(m_InterestMap.ToArray());
        int indexOfBiggestInterest = m_InterestMap.FindIndex(x => (x == biggestInterest));
        m_Rigibody2D.AddForce(m_Directions[indexOfBiggestInterest] * m_MovementSpeed * Time.deltaTime);
````

For a Directional context steering behavior that for example always want to go forward it will be neccessary to alter the rotation of the agent to the movement direction.
````
        Vector2 lookdirection = (m_Rigibody2D.velocity + agentPosition) - agentPosition;
        float angle = Mathf.Atan2(lookdirection.y, lookdirection.x) * Mathf.Rad2Deg - 90.0f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
````

### Base context behavior
The base context behavior is the base class where all other context steering behaviors are derived from. This abstract class specifies the GetInterestMap() and GetDangerMap() functions. These functions are to be implemented by derriving sub classes to create and return the interest and dangermap respectivly.
````
    abstract public List<float> GetInterestMap(Vector2 agentPostion, ref List<Vector2> directions);
    abstract public List<float> GetDangerMap(Vector2 agentPostion, ref List<Vector2> directions);
````
### Chase context behavior
This context behavior will calculate the interest and danger map for chasing down a target.
Important to note is that the danger map is filled with zeros because the chase behavior only has desires towards a target direction.
#### Member variables
m_MaxChaseDistance: Max distance to chase down a target
m_ChaseTargets: Array of targets to chase
````
    [SerializeField] private float m_MaxChaseDistance;
    private GameObject[] m_ChaseTargets;
````
#### GetInterestMap
Calculating the interest map based on the distance that the agent is from the target. How closer the agent is the higher the interest will be.
````
    public override List<float> GetInterestMap(Vector2 agentPosition, ref List<Vector2> directions)
    {
        m_InterestMap = new List<float>(new float[directions.Count]);
        foreach(GameObject target in m_ChaseTargets)
        {
            Vector2 targetPos = target.transform.position;
            Vector2 toTarget = targetPos - agentPosition;

            if (toTarget.magnitude > m_MaxChaseDistance)
                continue;

            for (int i = 0; i < directions.Count; i++)
            {
                float interestAmount = Vector2.Dot(toTarget, directions[i]) / toTarget.magnitude;
                Debug.Log(i);

                if (m_CenterBetweenTargets)
                {
                    m_InterestMap[i] = m_InterestMap[i] + interestAmount;
                }
                else
                {
                    if(interestAmount > m_InterestMap[i])
                    {
                        m_InterestMap[i] = interestAmount;
                    }
                }
            }
        }
        return m_InterestMap;
    }
````
### Avoid context behavior
The avoid context behavior does the exact oposite from the chase behavior. Instead of filling the interest map, this behavior will fill its danger map to make the context merger know that it doesn't want to go into a certain direction.
#### Member variables
m_MaxAvoidDistance: max distance to avoid a far away target to influence the dangermap
m_AvoidTarget: array of targets that the behavior wants to avoid
````
    [SerializeField] private float m_MaxAvoidDistance;
    private GameObject[] m_AvoidTargets;
````

#### GetDangerMap
````
public override List<float> GetDangerMap(Vector2 agentPosition, ref List<Vector2> directions)
    {

        m_DangerMap = new List<float>(new float[directions.Count]);

        foreach (GameObject avoidTarget in m_AvoidTargets)
        {
            Vector2 targetPos = avoidTarget.transform.position;
            Vector2 toTarget = targetPos - agentPosition;

            if (toTarget.magnitude > m_MaxAvoidDistance)
                continue;

            for (int i = 0; i < directions.Count; i++)
            {
                float dangerAmount = Vector2.Dot(toTarget, directions[i]) / toTarget.magnitude;
                Debug.Log(i);


                if (dangerAmount > m_DangerMap[i])
                {
                    m_DangerMap[i] = dangerAmount;
                }
            }
        }

        return m_DangerMap;
    }
````

### Directional context behavior
The directional context behavior is a very simple behavior that fills the interest context map to just go in the agents forward direction.
#### GetInterestMap
````
    public override List<float> GetInterestMap(Vector2 agentPostion, ref List<Vector2> directions)
    {
        m_InterestMap = new List<float>(new float[directions.Count]);

        for (int i = 0; i < directions.Count; i++)
        {
            m_InterestMap[i] = Vector2.Dot(gameObject.transform.up * 0.8f, directions[i]);
        }
        
        return m_InterestMap;
    }
````
## Result
### ContextSteering behaviour using Chase and avoid
In this example you can see the "Chase context behavior" in action together with the "avoid context behavior".
By combining these we can get a simple pathfinding agent that finds the target through a simple corridor with obstacles.
![ContextSteeringPathFinding](https://user-images.githubusercontent.com/41028126/151200242-e4261247-d152-46fb-8299-14b755f4c060.gif)

### ContextSteering behaviour Directional steering and avoid
In this example you can see the "directional context steering" and the "avoid context steering" in action.
This is a really nice example of the power of context steering. If you look at the implementation of these behaviors the are decoupled from eachother. The only thing the directional behavior needs to worry about is showing its desire to go forward. And the avoid only shows its unintend of being close to "avoid targets". But when these are combined we already get a seemingly smart AI agent that can traverse simple racetracks.
![ContextSteeringRacing](https://user-images.githubusercontent.com/41028126/151201575-8f0ae3fe-27a4-4245-b2f1-cb11f022bc0a.gif)

## Conclusion
Context steering behaviors have a simple system of decoupled behaviors that is easy to maintain and implement while providing decently impressive results even with a basic implementation. Though one should first consider wheter this method of steering behaviors is a good fit for the game they are making.
### Usage in games
Context steering has been used in a variaty of games including but not limmited to.
- F1 2011 by Codemasters 
## Sources
- Game AI 2 Chapter 18: Behavior-Driven steering at the macro scale
    by Andrew Fray
- AI Context behaviors
    https://jameskeats.com/portfolio/contextbhvr.html
- Context behaviours know how to share
    https://andrewfray.wordpress.com/2013/03/26/context-behaviours-know-how-to-share/
- Racing AI with Context Steering - Andrew Fray
    https://www.youtube.com/watch?v=2fg-th5cTpg

---

# Getting Started

## Check out the Demo scene

First things first, a demo scene has been included that displays some basic usage of this tool. When importing the package into your project you should see a tick box to include the sample scene. The scene is stored at: [**Samples/DemoScene/Scenes/DemoScene.unity**](https://github.com/friedforfun/ContextSteering/blob/master/Samples/DemoScene/Scenes/DemoScene.unity)

This scene provides some examples showing how you might use this package. Cyan cylinders represent *agents* which are using steering behaviours, Orange spheres are *projectiles* or obstacles to avoid, and Mauve objects represent *targets* the agents will try to reach.

Taking a look at an agent (depicted below) there are a few scripts to pay particular attention to: 

- **[SelfSchedulingPlanarController](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/Controllers/SelfSchedulingPlanarController.cs):** This is a type of **[PlanarSteeringController](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/PlanarSteeringBehaviour.cs)**, it is required for each agent that has any behaviours. It is the component that we use to get our output vector for use outside the package (for example to decide in which direction to move),  we define the parameters that are shared to all behaviours here.

- **[DotToTransform](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/Behaviours/DotToTransform.cs)/[DotToTag](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/Behaviours/DotToTag.cs):** These are examples of simple **Behaviours**, they all have an effective **Range**, name, and cruicially a **Direction** of effect. Behaviours that **ATTRACT** will weight the output vector towards their targets, **REPULSE** will weight the output vector away from the targets. The Position/Tag arrays on these components are how we select the targets of the behaviours.

- **[DotToLayerMask](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/Masks/DotToLayerMask.cs):** This is a **Mask**, these behave very similarly to behaviours, but rather than attract or repulse, they block (or mask out) a direction from being selected

![BasicAgent](Documentation~/images/DemoGuide/BasicAgent.png)

<small>*Also note the **[PlanarMovement](https://github.com/friedforfun/ContextSteering/blob/master/Samples/DemoScene/Scripts/Agent/PlanarMovement.cs)** demo script, when using this package you would likely create a similar script yourself to decide how to use the output vector in your game.*</small>

Each demo includes some basic metrics; total collisions, collisions in the last n seconds, average collisions per n seconds, and number of goals achieved (contact with correct Mauve objects). Play with the PlanarController, Behaviour, and Mask parameters to see how they each effect agents movement, collisions, and goals.

The Map Debugger when enabled can help provide some visual context for what each behaviour is doing. Note that the length of the lines in this visualisation are normalised to the Map Size so they are not a true representation of the internal data.

---

## Basic usage

### [Controllers](https://github.com/friedforfun/ContextSteering/tree/master/Runtime/PlanarMovement/Controllers)

This package is designed so that once you have configured the behaviours on your agent you can use the output vector (or direction) however you wish.   

The output vector is provided by a controller component, currently the only controller that has been implemented is the [**SelfSchedulingPlanarController**](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/Controllers/SelfSchedulingPlanarController.cs), but if you wish you can extend the [**PlanarSteeringController**](https://github.com/friedforfun/ContextSteering/blob/master/Runtime/PlanarMovement/PlanarSteeringController.cs) class and implement your own custom planar controller.  

[See the PlanarMovement demo script](https://github.com/friedforfun/ContextSteering/blob/master/Samples/DemoScene/Scripts/Agent/PlanarMovement.cs) for an example of interacting with a controller.  

**Controller Parameters**
- *Context Map Resolution* - The number of directions to evaluate, each direction is evenly spaced. In Euler angles 360/ContextMapResolution is the angle between each spoke of the map.
- *Context Map Rotation Axis* - The axis around which we define our plane of movement (The axis should be perpendicular to all directions being evaluated). Usually **Y-AXIS** for a 3D game, and **Z-AXIS** for a 2D game.

![Context Map Visualised](Documentation~/images/DemoGuide/ContextMapVis.png)

**SelfSchedulingPlanarController Parameters**
- *Ticks Per Second* - The number of times per second the agent updates its output vector, must be greater than 0.
- *Direction Selector* - This enum allows you to choose 2 algorithms that pick a direction from the final combined context map. **BASIC** just picks the direction with the highest weight, **WITH_INERTIA** caps the angle delta per tick based on the value of *MinDotPerTick*.
- *Min Dot Per Tick* - Applies only when **WITH_INERTIA** is selected as the direction algorithm. Specifies the lowest dot product between the last tick vector and the next output vector. For example, a value of 0 would allow at most a 90 degree change in direction per tick.

### [Behaviours](https://github.com/friedforfun/ContextSteering/tree/master/Runtime/PlanarMovement/Behaviours)

Inclued in this package are 4 Behaviour classes; **DotToTag, DotToTransform, DotToLayer, DotToNavMesh**. Each behaviour differs only in how an array of Vector3 positions is generated each time the controller "thinks", adding new planar behaviours is as simple as overriding the GetPositionVectors method.

**Commmon Behaviour Parameters**
- *Range* - The max range the behaviour considers target positions.
- *Name* - Just a name for the behaviour, might help with debugging. Can be left blank.
- *Direction* - Set to **ATTRACT** to make the behaviour try to move towards the targets, or **REPULSE** to make the behaviour avoid the targets. 
- *Weight* - Modifies the intensity of this behaviour relative to the others. Particularly useful in conjunction with *Scale on distance*.
- *Scale on distance* - Scale the weight of this behaviour based on the distance from the target.
- *Invert scale* - Only applies when *Scale on distance* is enabled. Set to **true** to increase the importance of targets as they approach, **false** makes distant targets more important (and reduces their importance as they approach).


### [Masks](https://github.com/friedforfun/ContextSteering/tree/master/Runtime/PlanarMovement/Masks)

There are 3 Mask classes; **DotToTagMask, DotToTransformMask, and DotToLayerMask**. Masks are very similar to behaviours but instead of attracting or repulsing they block (or mask out) a direction entirely. They can be useful in conjunction with a scale on distance Repulse behaviours to create a lower band on distance to an object (like a wall). Masks do not guarentee that an agent will not move into something masked out, but they do reduce the probability.

**Mask Parameters**

See [behaviour](#behaviours) Parameters

---

## Installation
Either download the latest [Release](https://github.com/friedforfun/ContextSteering/releases) .zip file, or change to the UPM branch and hit the green `Code` button and select `Download ZIP`.  
1. Open the Package Manager (under Window -> Package Manager).  

    ![OpenPackage](Documentation~/images/Installation/openpackagemanager.png)  

2. Click the "+" icon, and select "Add package from disk...".  

    ![PlusIcon](Documentation~/images/Installation/addpackagefromdisk.png)  

3. Browse to the directory this package is installed at and open the package.json file.  

    ![SelectPackageJSON](Documentation~/images/Installation/SelectPackageJson.png)
    
Alternatively you can add the package using the Git URL (`Code` -> `HTTPS`).

---
