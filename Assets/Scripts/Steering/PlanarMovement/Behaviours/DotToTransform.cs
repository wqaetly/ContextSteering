using UnityEngine;
using Friedforfun.SteeringBehaviours.Core;

namespace Friedforfun.SteeringBehaviours.PlanarMovement
{
    public class DotToTransform : PlanarSteeringBehaviour
    {
        [SerializeField] public Transform[] Positions;

        protected override Vector3[] getPositionVectors()
        {
            return VectorsFromTransformArray.GetVectors(Positions);
        }

    }

}
