using UnityEngine;

namespace AuthoritativeServer.Inputs
{
    public class TestInputStream : InputStream
    {
        protected override void Build(InputData data)
        {
            float inputX = Input.GetAxisRaw("Horizontal");

            float inputY = Input.GetAxisRaw("Vertical");

            data.Add(new FloatInput(inputX));

            data.Add(new FloatInput(inputY));
        }
    }
}
