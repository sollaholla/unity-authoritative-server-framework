using UnityEngine;

namespace AuthoritativeServer.Inputs
{
    public class TestOutputStream : InputStream
    {
        private Transform m_Player;

        public void SetPlayer(Transform player)
        {
            m_Player = player;
        }

        protected override void Build(InputData data)
        {
            data.Add(new Vector3Input(m_Player.position));

            data.Add(new FloatInput(m_Player.eulerAngles.y));
        }
    }
}
