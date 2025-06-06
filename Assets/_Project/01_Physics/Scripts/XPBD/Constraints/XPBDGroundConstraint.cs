using System.Collections.Generic;
using System.Linq;
using _Project._01_Physics.Scripts.XPBD.Core;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Constraints
{
    /// <summary>
    /// XPBD Ground Collision Constraint - handles bouncing behavior
    /// </summary>
    public class XPBDGroundConstraint : XPBDConstraint
    {
        public float GroundY = 0.0f;
        public float Restitution = 0.8f;
        public float Friction = 0.3f;

        public XPBDGroundConstraint(float groundY, float restitution = 0.8f, float friction = 0.3f)
        {
            GroundY = groundY;
            Restitution = restitution;
            Friction = friction;
            Compliance = 0.0f; // Infinitely stiff ground
        }

        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            foreach (var particle in particles.Where(particle => !particle.IsFixed)
                         .Where(particle => particle.PredictedPosition.y < GroundY))
            {
                // Position correction
                particle.PredictedPosition.y = GroundY;

                // Velocity correction for bouncing
                Vector3 velocity = particle.GetVelocity(deltaTime);
                if (velocity.y < 0)
                {
                    // Apply restitution
                    particle.PreviousPosition.y = particle.Position.y + velocity.y * Restitution * deltaTime;

                    // Apply friction
                    Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
                    horizontalVel *= (1.0f - Friction);
                    particle.PreviousPosition.x = particle.Position.x - horizontalVel.x * deltaTime;
                    particle.PreviousPosition.z = particle.Position.z - horizontalVel.z * deltaTime;
                }
            }
        }

        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            return particles.Where(particle => particle.PredictedPosition.y < GroundY)
                .Aggregate(0.0f, (current, particle)
                    => Mathf.Max(current, GroundY - particle.PredictedPosition.y));
        }
    }
}