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
        private readonly float _groundY;
        private readonly float _restitution;
        private readonly float _friction;

        public XPBDGroundConstraint(float groundY, float restitution = 0.8f, float friction = 0.3f)
        {
            _groundY = groundY;
            _restitution = restitution;
            _friction = friction;
            Compliance = 0.0f; // Infinitely stiff ground
        }

        public override void SolveConstraint(List<XPBDParticle> particles, float deltaTime)
        {
            foreach (var particle in particles.Where(particle => !particle.IsFixed)
                         .Where(particle => particle.PredictedPosition.y < _groundY))
            {
                // Position correction
                particle.PredictedPosition.y = _groundY;

                // Velocity correction for bouncing
                Vector3 velocity = particle.GetVelocity(deltaTime);
                if (velocity.y < 0)
                {
                    // Apply restitution
                    particle.PreviousPosition.y = particle.Position.y + velocity.y * _restitution * deltaTime;

                    // Apply friction
                    Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
                    horizontalVel *= (1.0f - _friction);
                    particle.PreviousPosition.x = particle.Position.x - horizontalVel.x * deltaTime;
                    particle.PreviousPosition.z = particle.Position.z - horizontalVel.z * deltaTime;
                }
            }
        }

        public override float EvaluateConstraint(List<XPBDParticle> particles)
        {
            return particles.Where(particle => particle.PredictedPosition.y < _groundY)
                .Aggregate(0.0f, (current, particle)
                    => Mathf.Max(current, _groundY - particle.PredictedPosition.y));
        }
    }
}