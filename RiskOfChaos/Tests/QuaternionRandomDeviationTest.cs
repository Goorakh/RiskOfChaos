using RiskOfChaos.Utilities;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Tests
{
#if DEBUG || true
    static class QuaternionRandomDeviationTest
    {
        [ConCommand(commandName = "roc_test_quaternion_random_deviation")]
        static void CCTestQuaternionRandomDeviation(ConCommandArgs args)
        {
            float firstFloatArg = args.GetArgFloat(0);
            float? secondFloatArg = args.TryGetArgFloat(1);

            float minAngle, maxAngle;
            if (secondFloatArg.HasValue)
            {
                minAngle = firstFloatArg;
                maxAngle = secondFloatArg.Value;
            }
            else
            {
                minAngle = 0f;
                maxAngle = firstFloatArg;
            }

            float averageAngle = 0f;

            float minGeneratedAngle = float.PositiveInfinity;
            float maxGeneratedAngle = float.NegativeInfinity;

            const int ITERATIONS = 1000;

            float[] angles = new float[ITERATIONS];

            for (int i = 0; i < ITERATIONS; i++)
            {
                Quaternion deviation = QuaternionUtils.RandomDeviation(minAngle, maxAngle, RoR2Application.rng);

                Vector3 vector = deviation * Vector3.forward;

                float angle = Vector3.Angle(vector, Vector3.forward);

                minGeneratedAngle = Mathf.Min(angle, minGeneratedAngle);
                maxGeneratedAngle = Mathf.Max(angle, maxGeneratedAngle);

                angles[i] = angle;
                averageAngle += angle;
            }

            Array.Sort(angles);

            averageAngle /= ITERATIONS;

            float minStep = float.PositiveInfinity;
            float maxStep = float.NegativeInfinity;

            for (int i = 1; i < ITERATIONS; i++)
            {
                float prevAngle = angles[i - 1];
                float angle = angles[i];

                float step = angle - prevAngle;

                minStep = Mathf.Min(step, minStep);
                maxStep = Mathf.Max(step, maxStep);
            }

            Debug.Log($"{minAngle}->{maxAngle} angle deviation generation ({ITERATIONS} iterations): Average={averageAngle}, MinValue={minGeneratedAngle}, MaxValue={maxGeneratedAngle}, MinStep={minStep}, MaxStep={maxStep}");
        }
    }
#endif
}
