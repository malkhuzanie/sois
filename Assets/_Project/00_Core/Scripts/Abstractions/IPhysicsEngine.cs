using System.Collections.Generic;
using _Project._00_Core.Scripts.DataStructures;
using UnityEngine;

namespace _Project._00_Core.Scripts.Abstractions
{
    /// <summary>
    /// Main physics engine interface
    /// </summary>
    public interface IPhysicsEngine
    {
        void AddObject(ISimulationObject obj);
        void RemoveObject(ISimulationObject obj);
        void RemoveObject(string objectID);
        
        void Step(float deltaTime);
        void Pause();
        void Resume();
        void Reset();
        
        List<CollisionInfo> GetCurrentCollisions();
        List<ISimulationObject> GetObjectsInRadius(Vector3 center, float radius);
        
        Vector3 Gravity { get; set; }
        float TimeScale { get; set; }
        bool IsRunning { get; }
        int ObjectCount { get; }
    }
}