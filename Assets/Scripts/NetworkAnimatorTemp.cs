//#if COM_UNITY_MODULES_ANIMATION
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


/// <summary>
/// NetworkAnimator enables remote synchronization of <see cref="UnityEngine.Animator"/> state for on network objects.
/// </summary>
[AddComponentMenu("Netcode/" + nameof(NetworkAnimatorTemp))]
[RequireComponent(typeof(Animator))]
public class NetworkAnimatorTemp : NetworkAnimator
{
    internal struct AnimationMessage : INetworkSerializable
    {
        // state hash per layer.  if non-zero, then Play() this animation, skipping transitions
        internal int StateHash;
        internal float NormalizedTime;
        internal int Layer;
        internal float Weight;
        internal byte[] Parameters;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref StateHash);
            serializer.SerializeValue(ref NormalizedTime);
            serializer.SerializeValue(ref Layer);
            serializer.SerializeValue(ref Weight);
            serializer.SerializeValue(ref Parameters);
        }
    }

    internal struct AnimationTriggerMessage : INetworkSerializable
    {
        internal int Hash;
        internal bool Reset;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hash);
            serializer.SerializeValue(ref Reset);
        }
    }

    private bool m_SendMessagesAllowed = false;
    private bool m_writtenOnce = false;

    // Animators only support up to 32 params
    private const int k_MaxAnimationParams = 32;

    private int[] m_TransitionHash;
    private int[] m_AnimationHash;
    private float[] m_LayerWeights;

    private unsafe struct AnimatorParamCache
    {
        internal int Hash;
        internal int Type;
        internal fixed byte Value[4]; // this is a max size of 4 bytes
    }

    // 128 bytes per Animator
    private FastBufferWriter m_ParameterWriter = new FastBufferWriter(k_MaxAnimationParams * sizeof(float), Allocator.Persistent);
    private NativeArray<AnimatorParamCache> m_CachedAnimatorParameters;

    // We cache these values because UnsafeUtility.EnumToInt uses direct IL that allows a non-boxing conversion
    private struct AnimationParamEnumWrapper
    {
        internal static readonly int AnimatorControllerParameterInt;
        internal static readonly int AnimatorControllerParameterFloat;
        internal static readonly int AnimatorControllerParameterBool;

        static AnimationParamEnumWrapper()
        {
            AnimatorControllerParameterInt = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Int);
            AnimatorControllerParameterFloat = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Float);
            AnimatorControllerParameterBool = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Bool);
        }
    }

    private void Update()
    {
        if (m_writtenOnce == false && NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.IsClient)
        {
            ForceSendValuesServerRpc();
        }
    }

    private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime, int layer)
    {
        bool shouldUpdate = false;
        stateHash = 0;
        normalizedTime = 0;

        float layerWeightNow = Animator.GetLayerWeight(layer);

        if (!Mathf.Approximately(layerWeightNow, m_LayerWeights[layer]))
        {
            m_LayerWeights[layer] = layerWeightNow;
            shouldUpdate = true;
        }
        if (Animator.IsInTransition(layer))
        {
            AnimatorTransitionInfo tt = Animator.GetAnimatorTransitionInfo(layer);
            if (tt.fullPathHash != m_TransitionHash[layer])
            {
                // first time in this transition for this layer
                m_TransitionHash[layer] = tt.fullPathHash;
                m_AnimationHash[layer] = 0;
                shouldUpdate = true;
            }
        }
        else
        {
            AnimatorStateInfo st = Animator.GetCurrentAnimatorStateInfo(layer);
            if (st.fullPathHash != m_AnimationHash[layer])
            {
                // first time in this animation state
                if (m_AnimationHash[layer] != 0)
                {
                    // came from another animation directly - from Play()
                    stateHash = st.fullPathHash;
                    normalizedTime = st.normalizedTime;
                }
                m_TransitionHash[layer] = 0;
                m_AnimationHash[layer] = st.fullPathHash;
                shouldUpdate = true;
            }
        }

        return shouldUpdate;
    }

    /* $AS TODO: Right now we are not checking for changed values this is because
    the read side of this function doesn't have similar logic which would cause
    an overflow read because it doesn't know if the value is there or not. So
    there needs to be logic to track which indexes changed in order for there
    to be proper value change checking. Will revist in 1.1.0.
    */
    private unsafe void WriteParameters(FastBufferWriter writer)
    {
        for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
        {
            ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), i);
            var hash = cacheValue.Hash;

            if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
            {
                var valueInt = Animator.GetInteger(hash);
                fixed (void* value = cacheValue.Value)
                {
                    UnsafeUtility.WriteArrayElement(value, 0, valueInt);
                    BytePacker.WriteValuePacked(writer, (uint)valueInt);
                }
            }
            else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
            {
                var valueBool = Animator.GetBool(hash);
                fixed (void* value = cacheValue.Value)
                {
                    UnsafeUtility.WriteArrayElement(value, 0, valueBool);
                    writer.WriteValueSafe(valueBool);
                }
            }
            else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
            {
                var valueFloat = Animator.GetFloat(hash);
                fixed (void* value = cacheValue.Value)
                {

                    UnsafeUtility.WriteArrayElement(value, 0, valueFloat);
                    writer.WriteValueSafe(valueFloat);
                }
            }
        }
    }

    private unsafe void ReadParameters(FastBufferReader reader)
    {
        for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
        {
            ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), i);
            var hash = cacheValue.Hash;

            if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
            {
                ByteUnpacker.ReadValuePacked(reader, out int newValue);
                Animator.SetInteger(hash, newValue);
                fixed (void* value = cacheValue.Value)
                {
                    UnsafeUtility.WriteArrayElement(value, 0, newValue);
                }
            }
            else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
            {
                reader.ReadValueSafe(out bool newBoolValue);
                Animator.SetBool(hash, newBoolValue);
                fixed (void* value = cacheValue.Value)
                {
                    UnsafeUtility.WriteArrayElement(value, 0, newBoolValue);
                }
            }
            else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
            {
                reader.ReadValueSafe(out float newFloatValue);
                Animator.SetFloat(hash, newFloatValue);
                fixed (void* value = cacheValue.Value)
                {
                    UnsafeUtility.WriteArrayElement(value, 0, newFloatValue);
                }
            }
        }
    }

    /// <summary>
    /// Internally-called RPC client receiving function to update some animation parameters on a client when
    ///   the server wants to update them
    /// </summary>
    /// <param name="animSnapshot">the payload containing the parameters to apply</param>
    /// <param name="clientRpcParams">unused</param>
    [ClientRpc]
    private unsafe void SendAnimStateClientRpc(AnimationMessage animSnapshot, ClientRpcParams clientRpcParams = default)
    {
        if (animSnapshot.StateHash != 0)
        {
            Animator.Play(animSnapshot.StateHash, animSnapshot.Layer, animSnapshot.NormalizedTime);
        }
        Animator.SetLayerWeight(animSnapshot.Layer, animSnapshot.Weight);

        if (animSnapshot.Parameters != null && animSnapshot.Parameters.Length != 0)
        {
            // We use a fixed value here to avoid the copy of data from the byte buffer since we own the data
            fixed (byte* parameters = animSnapshot.Parameters)
            {
                var reader = new FastBufferReader(parameters, Allocator.None, animSnapshot.Parameters.Length);
                ReadParameters(reader);
            }
        }
    }

    [ServerRpc]
    private void ForceSendValuesServerRpc()
    {
        if (!m_SendMessagesAllowed || !Animator || !Animator.enabled)
        {
            return;
        }

        for (int layer = 0; layer < Animator.layerCount; layer++)
        {
            int stateHash;
            float normalizedTime;
            CheckAnimStateChanged(out stateHash, out normalizedTime, layer);

            var animMsg = new AnimationMessage
            {
                StateHash = stateHash,
                NormalizedTime = normalizedTime,
                Layer = layer,
                Weight = m_LayerWeights[layer]
            };

            m_ParameterWriter.Seek(0);
            m_ParameterWriter.Truncate();

            WriteParameters(m_ParameterWriter);
            animMsg.Parameters = m_ParameterWriter.ToArray();

            SendAnimStateClientRpc(animMsg);
        }
    }
}
//#endif // COM_UNITY_MODULES_ANIMATION