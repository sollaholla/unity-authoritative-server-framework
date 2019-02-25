using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AuthoritativeServer.Inputs
{
    public abstract class InputTypeBase
    {
        public abstract byte[] Serialize();

        public abstract void Deserialize(byte[] data);

        public abstract bool Compare(InputTypeBase other);
    }

    public abstract class InputType<T> : InputTypeBase
    {
        public InputType() { }

        public InputType(T value)
        {
            Value = value;
        }

        public T Value { get; set; }

        public abstract override byte[] Serialize();

        public abstract override void Deserialize(byte[] data);

        public static implicit operator T(InputType<T> other)
        {
            return other.Value;
        }
    }

    public class IntInput : InputType<short>
    {
        public IntInput() { }

        public IntInput(short value) : base(value)
        { }

        public override void Deserialize(byte[] data)
        {
            Value = BitConverter.ToInt16(data, 0);
        }

        public override byte[] Serialize()
        {
            return BitConverter.GetBytes(Value);
        }

        public override bool Compare(InputTypeBase other)
        {
            if (other is IntInput f)
            {
                return f.Value == Value;
            }

            return false;
        }
    }

    public class FloatInput : InputType<float>
    {
        public FloatInput() { }

        public FloatInput(float value) : base(value)
        { }

        public override void Deserialize(byte[] data)
        {
            Value = BitConverter.ToSingle(data, 0);
        }

        public override byte[] Serialize()
        {
            return BitConverter.GetBytes(Value);
        }

        public override bool Compare(InputTypeBase other)
        {
            if (other is FloatInput f)
            {
                return f.Value == Value;
            }

            return false;
        }
    }

    public class BoolInput : InputType<bool>
    {
        public BoolInput() { }

        public BoolInput(bool value) : base(value)
        { }

        public override bool Compare(InputTypeBase other)
        {
            if (other is BoolInput b)
            {
                return b.Value == Value;
            }

            return false;
        }

        public override void Deserialize(byte[] data)
        {
            Value = BitConverter.ToBoolean(data, 0);
        }

        public override byte[] Serialize()
        {
            return BitConverter.GetBytes(Value);
        }
    }

    public class Vector3Input : InputType<Vector3>
    {
        public Vector3Input() { }

        public Vector3Input(Vector3 value) : base(value)
        { }

        public override void Deserialize(byte[] data)
        {
            NetworkWriter writer = new NetworkWriter(data);
            Value = writer.ReadVector3();
        }

        public override byte[] Serialize()
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write(Value);
            return writer.ToArray();
        }

        public override bool Compare(InputTypeBase other)
        {
            if (other is Vector3Input v)
            {
                return v.Value == Value;
            }

            return false;
        }
    }

    public class TriggerInput : InputType<bool>
    {
        public TriggerInput() { }

        public TriggerInput(bool value) : base(value)
        { }

        public override void Deserialize(byte[] data)
        {
            NetworkWriter writer = new NetworkWriter(data);
            Value = writer.ReadBool();
        }

        public override byte[] Serialize()
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write(Value);
            return writer.ToArray();
        }

        public override bool Compare(InputTypeBase other)
        {
            if (other is TriggerInput v)
            {
                return v.Value == Value;
            }

            return false;
        }
    }

    public class InputData
    {
        private List<Type> m_ExpectedInputs;

        public InputData()
        {
            Inputs = new List<InputTypeBase>();
            Similar = new List<InputData>();
            m_ExpectedInputs = new List<Type>();
        }

        public InputData(float time) : this()
        {
            Time = time;
        }

        /// <summary>
        /// The input time.
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// The inputs.
        /// </summary>
        public List<InputTypeBase> Inputs { get; private set; }

        /// <summary>
        /// The similar data.
        /// </summary>
        public List<InputData> Similar { get; }

        /// <summary>
        /// Add an input.
        /// </summary>
        /// <param name="input"></param>
        public void Add(InputTypeBase input)
        {
            Inputs.Add(input);
        }

        /// <summary>
        /// Add similar data to this input.
        /// </summary>
        /// <param name="data"></param>
        public void AddSimilar(InputData data)
        {
            Similar.Add(data);
        }

        /// <summary>
        /// Get the input as bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] GetData()
        {
            NetworkWriter writer = new NetworkWriter();

            writer.Write(Time);

            writer.Write((short)Inputs.Count);
            foreach (var input in Inputs)
            {
                writer.WriteBytesAndSize(input.Serialize());
            }

            writer.Write((short)Similar.Count);
            foreach (var similar in Similar)
            {
                writer.Write(similar.Time);
            }

            return writer.ToArray();
        }

        public void SetExpected(params Type[] inputTypes)
        {
            foreach (var t in inputTypes)
            {
                m_ExpectedInputs.Add(t);
            }
        }

        public void ReadData(byte[] data)
        {
            NetworkWriter writer = new NetworkWriter(data);

            Time = writer.ReadSingle();

            int count = writer.ReadInt16();
            for (int i = 0; i < count; i++)
            {
                InputTypeBase input = (InputTypeBase)Activator.CreateInstance(m_ExpectedInputs[i]);
                int bCount = writer.ReadInt16();
                input.Deserialize(writer.ReadBytes(bCount));
                Inputs.Add(input);
            }

            int simCount = writer.ReadInt16();
            for (int i = 0; i < simCount; i++)
            {
                float time = writer.ReadSingle();
                InputData input = new InputData(time)
                {
                    Inputs = this.Inputs
                };
                Similar.Add(input);
            }
        }

        /// <summary>
        /// Compare with another input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool Compare(InputData input)
        {
            if (input.Inputs.Count != Inputs.Count)
                return false;

            for(int i = 0; i < Inputs.Count; i++)
            {
                InputTypeBase data = Inputs[i];

                InputTypeBase other = input.Inputs[i];

                if (data.GetType() != other.GetType())
                {
                    return false;
                }

                if (!data.Compare(other))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get input of type at the specified index. Check the <see cref="InputStream.Build(InputData)"/> for the order that inputs are built.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetInput<T> (int index) where T : InputTypeBase
        {
            return Inputs[index] as T;
        }
    }

    /// <summary>
    /// An easy wrapper for writing and reading inputs to bytes. Optimized to compress duplicates.
    /// </summary>
    public abstract class InputStream
    {
        private List<InputData> m_InputData;

        private Queue<InputData> m_ReadData;

        private Type[] m_ExpectedTypes;

        /// <summary>
        /// Add your inputs to data.
        /// </summary>
        /// <param name="data"></param>
        protected abstract void Build(InputData data);

        /// <summary>
        /// Collect input values.
        /// </summary>
        /// <param name="time">The timestamp.</param>
        public InputData GetInput(float time, bool add = true, bool overriteLast = false)
        {
            if (m_InputData == null)
                m_InputData = new List<InputData>();

            InputData data = new InputData(time);

            Build(data);

            if (m_InputData.Count > 0)
            {
                InputData last = m_InputData[m_InputData.Count - 1];

                for (int i = 0; i < last.Inputs.Count; i++)
                {
                    InputTypeBase inputType = last.Inputs[i];

                    if (inputType is TriggerInput t)
                    {
                        if (t.Value)
                        {
                            TriggerInput other = data.Inputs[i] as TriggerInput;

                            other.Value = t.Value;
                        }
                    }
                }

                if (overriteLast)
                {
                    m_InputData[m_InputData.Count - 1] = data;
                }

                if (last.Compare(data))
                {
                    if (add) last.AddSimilar(data);
                    return data;
                }
            }

            if (add) m_InputData.Add(data);

            return data;
        }

        /// <summary>
        /// Serialize input data.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            if (m_InputData == null || m_InputData.Count <= 0)
                return null;

            NetworkWriter writer = new NetworkWriter();

            writer.Write((short)m_InputData.Count);

            for (int i = 0; i < m_InputData.Count; i++)
            {
                InputData inputData = m_InputData[i];

                writer.WriteBytesAndSize(inputData.GetData());
            }

            m_InputData.Clear();

            return writer.ToArray();
        }

        /// <summary>
        /// Deserialize input data.
        /// </summary>
        /// <param name="data"></param>
        public void Deserialize(byte[] data)
        {
            if (m_ReadData == null)
            {
                m_ReadData = new Queue<InputData>();
            }

            Type[] expectedTypes = GetExpectedInputTypes();

            NetworkWriter writer = new NetworkWriter(data);

            int count = writer.ReadInt16();

            for (int i = 0; i < count; i++)
            {
                int bCount = writer.ReadInt16();

                byte[] inputData = writer.ReadBytes(bCount);

                InputData input = new InputData();

                input.SetExpected(expectedTypes);

                input.ReadData(inputData);

                m_ReadData.Enqueue(input);

                for (int j = 0; j < input.Similar.Count; j++)
                {
                    InputData similar = input.Similar[j];

                    m_ReadData.Enqueue(similar);
                }

                input.Similar.Clear();
            }
        }

        /// <summary>
        /// Dequeues the next received input.
        /// </summary>
        /// <returns></returns>
        public InputData ReceiveNext()
        {
            if (m_ReadData == null)
                return null;

            if (m_ReadData.Count == 0)
                return null;

            return m_ReadData?.Dequeue();
        }

        /// <summary>
        /// Retrieves the expected input types based on the <see cref="Build(InputData)"/> function.
        /// </summary>
        /// <returns></returns>
        private Type[] GetExpectedInputTypes()
        {
            if (m_ExpectedTypes != null)
                return m_ExpectedTypes;

            InputData temp = new InputData();
            Build(temp);
            m_ExpectedTypes = new Type[temp.Inputs.Count];
            for (int i = 0; i < temp.Inputs.Count; i++)
            {
                InputTypeBase tempData = temp.Inputs[i];
                m_ExpectedTypes[i] = tempData.GetType();
            }

            return m_ExpectedTypes;
        }
    }
}
