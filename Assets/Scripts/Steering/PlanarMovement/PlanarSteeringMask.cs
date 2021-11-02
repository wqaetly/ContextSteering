using UnityEngine;
using Friedforfun.SteeringBehaviours.Core;
using Friedforfun.SteeringBehaviours.Utilities;
using Unity.Collections;
using System;

namespace Friedforfun.SteeringBehaviours.PlanarMovement
{
    public abstract class PlanarSteeringMask : CoreSteeringMask<PlanarSteeringParameters>
    {
        [SerializeField] protected float Weight = 1f;
        [SerializeField] protected bool ScaleOnDistance = false;
        [SerializeField] bool InvertScale = true;

        protected float invertScalef { get { return InvertScale ? 1f : 0f; } }

        protected float[] maskMap; //The map of mask weights, each element represents our degree of interest in the direction that element corresponds to.
        protected PlanarSteeringParameters steeringParameters;

        [SerializeField] public PlanarMapVisualiserParameters MapDebugger;

        private NativeArray<float> nextMap;
        private NativeArray<Vector3> targetPositions;
        //private bool rebuildMap = false;

        public override void InstantiateMaskMap(PlanarSteeringParameters steeringParameters)
        {
            this.steeringParameters = steeringParameters;
            //this.steeringParameters.OnResolutionChange += MapResolutionChangeHandler;
            maskMap = new float[steeringParameters.ContextMapResolution];
            nextMap = new NativeArray<float>(steeringParameters.ContextMapResolution, Allocator.Persistent);
        }

        private void MapResolutionChangeHandler(object sender, EventArgs e)
        {
            //rebuildMap = true;
        }

        /// <summary>
        /// Return a context map where the index of the float defines the direction whose value is being inspected, the size of the scalar defines how much we want to move in a direction
        /// </summary>
        /// <returns></returns>
        public virtual float[] GetMaskMap()
        {
            return maskMap;
        }

        public DotToVecJob GetJob()
        {
            /*if (rebuildMap)
            {
                rebuildMap = false;
                nextMap = new NativeArray<float>(steeringParameters.ContextMapResolution, Allocator.Persistent);
            }*/

            Vector3[] targetArr = getPositionVectors();

            targetPositions = new NativeArray<Vector3>(targetArr.Length, Allocator.Persistent);

            for (int i = 0; i < targetArr.Length; i++)
            {
                targetPositions[i] = targetArr[i];
            }

            return new DotToVecJob()
            {
                targets = targetPositions,
                my_position = transform.position,
                range = Range,
                weight = Weight,
                angle = steeringParameters.ResolutionAngle,
                Weights = nextMap,
                direction = SteerDirection.ATTRACT, // Always attract because a mask points toward danger and masks out the highest danger
                scaled = ScaleOnDistance,
                invertScale = invertScalef,
                axis = steeringParameters.ContextMapRotationAxis
            };
        }

        public void Swap()
        {
            if (targetPositions.IsCreated)
            {
                targetPositions.Dispose();
            }

            float[] next = new float[nextMap.Length];
            for (int i = 0; i < nextMap.Length; i++)
            {
                next[i] = nextMap[i];
            }

            maskMap = next;
        }

        public virtual void OnDisable()
        {


            if (nextMap.IsCreated)
                nextMap.Dispose();

            if (targetPositions.IsCreated)
                targetPositions.Dispose();
        }

        protected abstract Vector3[] getPositionVectors();

        protected Quaternion rotateAroundAxis(float resolutionAngle)
        {
            return MapOperations.RotateAroundAxis(steeringParameters.ContextMapRotationAxis, resolutionAngle);
        }

        //protected virtual void OnDestroy()
        //{
        //    steeringParameters.OnResolutionChange -= MapResolutionChangeHandler;
        //}


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (maskMap != null && MapDebugger != null)
            {
                MapVisualiserPlanar.InDrawGizmos(MapDebugger, maskMap, Range, steeringParameters.ResolutionAngle, transform);
            }
        }
#endif
    }
}

