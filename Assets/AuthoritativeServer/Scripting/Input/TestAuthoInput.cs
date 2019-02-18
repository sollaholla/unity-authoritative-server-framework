using System.Collections.Generic;
using UnityEngine;

namespace AuthoritativeServer.Inputs
{
    public class TestAuthoInput : AuthoritativeInput<TestInputStream, TestOutputStream>
    {
        protected override void Awake()
        {
            base.Awake();

            m_ServerStream.SetPlayer(transform);
        }

        protected override void ExecuteInput(InputData input)
        {
            float hori = input.GetInput<FloatInput>(0).Value;

            float vert = input.GetInput<FloatInput>(1).Value;

            transform.Translate(new Vector3(hori, 0, vert) * Time.fixedDeltaTime, Space.Self);
        }

        protected override void UpdateSimulation(InputData input, InputData prediction, List<InputData> rewind)
        {
            Vector3 position = input.GetInput<Vector3Input>(0).Value;

            if (IsOwner)
            {
                Vector3 predictedPosition = prediction.GetInput<Vector3Input>(0).Value;

                const float ERR = 0.00001f;

                float distance = Vector3.Distance(predictedPosition, position);

                if (distance > ERR)
                {
                    Debug.Log("Do client replay...");
                }
            }
            else
            {
                transform.position = position;
            }
        }
    }
}
