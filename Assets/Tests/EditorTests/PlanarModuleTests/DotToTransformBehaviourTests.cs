using NUnit.Framework;
using UnityEngine;
using Friedforfun.SteeringBehaviours.Utilities;
using Friedforfun.SteeringBehaviours.PlanarMovement;
using Unity.Collections;

namespace Friedforfun.SteeringBehaviours.Tests
{
    public class DotToTransformTests
    {

        GameObject agent;
        GameObject target;
        DotToTransform behaviour;

        PlanarSteeringParameters planarParameters = new PlanarSteeringParameters();

        [SetUp]
        public void Setup()
        {
            agent = new GameObject();
            behaviour = agent.AddComponent<DotToTransform>();
            agent.transform.position = new Vector3(0, 0, 0);

            planarParameters.ContextMapRotationAxis = RotationAxis.YAxis;
            planarParameters.InitialVector = new Vector3(0, 0, 1);
            planarParameters.ContextMapResolution = 4;
            behaviour.BehaviourName = "Test behaviour";

            behaviour.InstantiateContextMap(planarParameters);

            target = new GameObject();
            target.transform.position = new Vector3(0, 0, 5);

            behaviour.Positions = new Transform[] { target.transform };
        }

        [TearDown]
        public void TearDown()
        {
            behaviour.OnDisable();
        }

        [Test]
        public void GetJobTest()
        {
            var job = behaviour.GetJob();

            var expectedTargets = new NativeArray<Vector3>(1, Allocator.Persistent);
            expectedTargets[0] = new Vector3(0, 0, 5);

            Assert.AreEqual(expectedTargets[0], job.targets[0]);

            Assert.AreEqual(new Vector3(0, 0, 0), job.my_position);

            Assert.AreEqual(10f, job.range);

            Assert.AreEqual(1f, job.weight);

            Assert.AreEqual(90, job.angle);

            Assert.AreEqual(SteerDirection.ATTRACT, job.direction);

            Assert.AreEqual(false, job.scaled);

            Assert.AreEqual(1f, job.invertScale);

            Assert.AreEqual(RotationAxis.YAxis, job.axis);

            // call swap to dispose of internal native array allocation
            behaviour.Swap();

            GameObject.DestroyImmediate(agent);
            expectedTargets.Dispose();
        }

        [Test]
        public void SwapTest()
        {
            var beforeMap = behaviour.GetContextMap();
            var expectedBefore = new float[] { 0f, 0f, 0f, 0f };
            Assert.AreEqual(expectedBefore, beforeMap);

            var job = behaviour.GetJob();
            job.Execute();

            behaviour.Swap();
            var result = behaviour.GetContextMap();
            var expectedAfter = new float[] { 1f, 0f, -1f, 0f };
            Assert.AreEqual(expectedAfter[0], result[0], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedAfter[1], result[1], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedAfter[2], result[2], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedAfter[3], result[3], TestUtilities.DOTPRODTOLERANCE);
        }

        [Test]
        public void TargetMoves()
        {
            var job = behaviour.GetJob();
            job.Execute();

            behaviour.Swap();
            var result = behaviour.GetContextMap();
            var expectedAfter = new float[] { 1f, 0f, -1f, 0f };
            Assert.AreEqual(expectedAfter[0], result[0], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedAfter[1], result[1], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedAfter[2], result[2], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedAfter[3], result[3], TestUtilities.DOTPRODTOLERANCE);

            target.transform.position = new Vector3(5f, 0, 0);

            job = behaviour.GetJob();
            job.Execute();
            behaviour.Swap();

            result = behaviour.GetContextMap();
            var expectedFinal = new float[] { 0f, 1f, 0f, -1f };
            Assert.AreEqual(expectedFinal[0], result[0], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedFinal[1], result[1], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedFinal[2], result[2], TestUtilities.DOTPRODTOLERANCE);
            Assert.AreEqual(expectedFinal[3], result[3], TestUtilities.DOTPRODTOLERANCE);
        }

    }

}