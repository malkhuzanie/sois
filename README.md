# XPBD Physics System Architecture Guide

## Table of Contents

1. [What is XPBD?](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#what-is-xpbd)
2. [Core Components Overview](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#core-components-overview)
3. [XPBDParticle: The Building Block](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#xpbdparticle-the-building-block)
4. [XPBDConstraint: The Rules of Physics](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#xpbdconstraint-the-rules-of-physics)
5. [XPBDSolver: The Simulation Engine](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#xpbdsolver-the-simulation-engine)
6. [Constraint Types Explained](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#constraint-types-explained)
7. [The Complete Simulation Loop](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#the-complete-simulation-loop)
8. [Material System](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#material-system)
9. [Integration with Unity](https://claude.ai/chat/676faf3c-f155-4376-b927-1981d58ad999#integration-with-unity)

---

## What is XPBD?

**XPBD (eXtended Position Based Dynamics)** is a physics simulation method that builds upon Position Based Dynamics (PBD) with a crucial improvement: **time-step independence**.

### The Core Philosophy

Traditional physics engines work with forces. You apply a force to an object, and it accelerates according to Newton's laws. XPBD takes a different approach: instead of computing forces, it directly manipulates positions to satisfy physical constraints.

Think of it this way: imagine you have a rubber ball. Instead of calculating all the internal forces keeping the ball's shape, XPBD says "these particles should stay roughly this distance apart" and directly moves particles to maintain that constraint.

### Why XPBD Over Regular PBD?

The key innovation in XPBD is **compliance** - a parameter that controls how "stiff" or "soft" a constraint is, and crucially, this stiffness remains consistent regardless of how fast or slow your simulation runs. This solves PBD's main weakness: results that change dramatically with different frame rates.

---

## Core Components Overview

The XPBD system consists of three fundamental building blocks:

```
XPBDParticle ──→ Basic physics points with mass and position
       ↓
XPBDConstraint ──→ Rules that govern how particles interact
       ↓
XPBDSolver ──→ Engine that updates everything each frame
```

Think of particles as dots, constraints as invisible springs or rules connecting those dots, and the solver as the brain that makes everything move realistically.

---

## XPBDParticle: The Building Block

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Core/XPBDParticle.cs`**

### What is a Particle?

In physics simulation, a particle is the simplest possible object - a point in space with mass but no size. Real objects are made up of many particles connected together.

### The Three Positions

Each XPBD particle tracks three positions, which is the key to Verlet integration:

```csharp
public Vector3 Position;          // Where the particle is now
public Vector3 PreviousPosition;  // Where it was last frame
public Vector3 PredictedPosition; // Where it wants to go next
```

### Why Three Positions?

This might seem redundant, but it's brilliant:

1. **Current Position**: The particle's actual location
2. **Previous Position**: Used to calculate velocity implicitly (velocity = (current - previous) / time)
3. **Predicted Position**: Where the particle would go if no constraints existed

### The Physics Behind Verlet Integration

Verlet integration is incredibly stable because it uses position history instead of explicitly storing velocity:

```
New Position = 2 × Current Position - Previous Position + Acceleration × (Time²)
```

This automatically includes velocity and acceleration in one elegant equation. The beauty is that small numerical errors don't accumulate as quickly as in other integration methods.

### Mass and Fixed Particles

```csharp
public float InverseMass = 1.0f;  // 1/mass (infinite mass = 0)
public bool IsFixed = false;      // Immovable objects
```

We store inverse mass because it's more efficient (no division operations) and naturally handles infinite mass (immovable objects have InverseMass = 0).

---

## XPBDConstraint: The Rules of Physics

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Constraints/XPBDConstraint.cs`**

### What is a Constraint?

A constraint is a rule that particles must follow. Think of it as saying "these two particles should always be exactly 1 meter apart" or "this particle cannot go below the ground."

### The XPBD Innovation: Compliance

```csharp
public float Compliance = 0.0f; // 1/stiffness
```

Compliance is the inverse of stiffness. A compliance of 0 means infinitely stiff (completely rigid), while higher values mean softer, more flexible constraints.

The key insight: compliance makes constraints behave consistently regardless of frame rate. A soft rubber ball will feel equally soft whether your game runs at 30 FPS or 120 FPS.

### How Constraints Work

Every constraint implements two key methods:

1. **EvaluateConstraint()**: "How much is this rule being violated?"
2. **SolveConstraint()**: "Move particles to satisfy this rule"

The solving process is iterative - we don't solve everything perfectly in one step, but gradually move particles closer to satisfying all constraints.

---

## XPBDSolver: The Simulation Engine

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Core/XPBDSolver.cs`**

### The Solver's Job

The solver is the conductor of the physics orchestra. Every frame, it:

1. Predicts where particles want to go
2. Applies all constraints to correct those predictions
3. Updates the actual positions
4. Applies damping to reduce unwanted oscillations

### Sub-stepping for Stability

```csharp
public int SubSteps = 4;
```

Instead of taking one large time step, the solver takes several smaller ones. This dramatically improves stability. Think of it like walking down stairs - you don't jump from top to bottom, you take it one step at a time.

### Iterative Constraint Solving

```csharp
public int SolverIterations = 8;
```

The solver doesn't solve all constraints perfectly in one pass. Instead, it applies each constraint partially, multiple times. This is like gradually tightening all the screws on a wheel rather than fully tightening one at a time.

### The Complete Update Process

```csharp
void SimulationStep(float deltaTime)
{
    // Phase 1: Predict where particles want to go
    foreach (var particle in Particles)
        particle.PredictPosition(Gravity, deltaTime);
    
    // Phase 2: Apply constraints iteratively
    for (int iteration = 0; iteration < SolverIterations; iteration++)
        foreach (var constraint in Constraints)
            constraint.SolveConstraint(Particles, deltaTime);
    
    // Phase 3: Update actual positions and apply damping
    foreach (var particle in Particles)
    {
        particle.UpdatePosition();
        particle.ApplyDamping(GlobalDamping);
    }
}
```

---

## Constraint Types Explained

### Distance Constraints

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Constraints/XPBDDistanceConstraint.cs`**

Distance constraints maintain the length between two particles. They're the backbone of solid objects.

**Physics Principle**: Two particles connected by a distance constraint will always try to maintain their rest length, like an invisible spring.

**Real-world analogy**: The bonds between atoms in a solid material.

### Volume Constraints

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Constraints/XPBDVolumeConstraint.cs`**

Volume constraints prevent objects from collapsing or expanding unrealistically.

**Physics Principle**: Many materials resist compression. A rubber ball might deform when squeezed but maintains roughly the same volume.

**Implementation**: Calculates the current volume of a group of particles and applies corrections to maintain the target volume.

### Ground Constraints

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Constraints/StableGroundConstraintV3.cs`**

Ground constraints handle collision with the ground plane, including bouncing and friction.

**Physics Principle**: Objects cannot penetrate solid surfaces, and energy is lost during collisions (restitution), while friction opposes sliding motion.

**Key Features**:

- **Penetration correction**: Pushes particles above the ground
- **Restitution**: Controls how bouncy collisions are
- **Friction**: Gradually slows down sliding objects

---

## The Complete Simulation Loop

Here's how everything works together each frame:

### 1. Prediction Phase

Each particle predicts where it wants to move based on its current velocity and external forces (like gravity):

```
Predicted Position = Current Position + Velocity × Time + ½ × Gravity × Time²
```

### 2. Constraint Solving Phase

For each iteration, every constraint examines the predicted positions and applies corrections. For example, a distance constraint might say:

"These two particles are 1.2 meters apart, but they should be 1.0 meters apart. I'll move each particle 0.1 meters toward each other."

### 3. Position Update Phase

The corrected predicted positions become the new actual positions, and velocities are derived:

```
New Velocity = (New Position - Old Position) / Time
```

### 4. Damping Phase

Small amounts of energy are removed to prevent unrealistic oscillations:

```
Velocity = Velocity × DampingFactor  // DampingFactor slightly less than 1.0
```

---

## Material System

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Materials/ElasticMaterial.cs`**

### Physical Properties to Simulation Parameters

The material system translates real-world material properties into simulation parameters:

- **Young's Modulus** → **Compliance values** for constraints
- **Density** → **Particle masses**
- **Poisson's Ratio** → **Volume constraint strength**

### Compliance Calculation

The key insight is converting material stiffness into compliance:

```csharp
public float CalculateDistanceCompliance()
{
    float stiffnessScale = 50000f;
    return 1.0f / (youngModulus * stiffnessScale);
}
```

Softer materials (lower Young's modulus) get higher compliance values, making their constraints more flexible.

---

## Integration with Unity

**File: `Assets/_Project/01_Physics/Scripts/XPBD/Components/XPBDRubberBall.cs`**

### The Unity Component Bridge

The `XPBDRubberBall` component acts as a bridge between Unity's GameObject system and the XPBD physics system:

1. **Mesh Generation**: Creates a sphere mesh optimized for physics simulation
2. **Particle Creation**: Converts mesh vertices into XPBD particles
3. **Constraint Setup**: Adds distance and volume constraints based on mesh topology
4. **Visual Updates**: Updates the rendered mesh each frame based on particle positions

### The Rendering Update Loop

```csharp
void UpdateMeshFromParticles()
{
    // Convert particle world positions back to local mesh coordinates
    for (int i = 0; i < deformedVertices.Length; i++)
        deformedVertices[i] = transform.InverseTransformPoint(solver.Particles[i].Position);
    
    // Update Unity's mesh
    deformedMesh.vertices = deformedVertices;
    deformedMesh.RecalculateNormals();
}
```

This creates the visual feedback where you see the mesh deform as the underlying particle simulation runs.

---

## Why This Architecture Works

### Modularity

Each component has a clear responsibility:

- **Particles**: Store state
- **Constraints**: Enforce rules
- **Solver**: Coordinate updates
- **Materials**: Define properties

### Extensibility

Adding new constraint types is straightforward - just inherit from `XPBDConstraint` and implement the two key methods.

### Performance

The system is designed for real-time performance:

- Minimal memory allocations during simulation
- Simple mathematical operations
- Efficient iterative solving

### Stability

XPBD's compliance-based approach provides robust simulation that doesn't "explode" easily, making it ideal for interactive applications.

This architecture successfully bridges the gap between accurate physics simulation and practical real-time performance, making it perfect for applications like games, training simulations, and interactive visualizations.
