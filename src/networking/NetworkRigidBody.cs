using System.Collections.Generic;
using System.Net.Sockets;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Wraps a boilerplate for syncing the position and rotation of a Rigidbody. Any locked axis will not be synced
///   to minimize bandwidth usage.
/// </summary>
public abstract class NetworkRigidBody : RigidBody, INetworkEntity
{
    protected Queue<StateSnapshot> stateInterpolations = new();
    protected float lerpTimer;

    private StateSnapshot? fromState;

    [JsonIgnore]
    public Spatial EntityNode => this;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    public abstract string ResourcePath { get; }

    public uint NetworkEntityId { get; set; }

    public void NetworkTick(float delta)
    {
        InterpolateStates(delta);
    }

    public virtual void NetworkSerialize(BytesBuffer buffer)
    {
        var bools = new bool[6]
        {
            AxisLockLinearX,
            AxisLockLinearY,
            AxisLockLinearZ,
            AxisLockAngularX,
            AxisLockAngularY,
            AxisLockAngularZ,
        };
        buffer.Write(bools.ToByte());

        if (!AxisLockLinearX)
            buffer.Write(GlobalTranslation.x);

        if (!AxisLockLinearY)
            buffer.Write(GlobalTranslation.y);

        if (!AxisLockLinearZ)
            buffer.Write(GlobalTranslation.z);

        if (!AxisLockAngularX)
            buffer.Write(GlobalRotation.x);

        if (!AxisLockAngularY)
            buffer.Write(GlobalRotation.y);

        if (!AxisLockAngularZ)
            buffer.Write(GlobalRotation.z);
    }

    public virtual void NetworkDeserialize(BytesBuffer buffer)
    {
        var bools = buffer.ReadByte();

        float xPos, yPos, zPos, xRot, yRot, zRot;
        xPos = yPos = zPos = xRot = yRot = zRot = 0;

        if (!bools.ToBoolean(0))
            xPos = buffer.ReadSingle();

        if (!bools.ToBoolean(1))
            yPos = buffer.ReadSingle();

        if (!bools.ToBoolean(2))
            zPos = buffer.ReadSingle();

        if (!bools.ToBoolean(3))
            xRot = buffer.ReadSingle();

        if (!bools.ToBoolean(4))
            yRot = buffer.ReadSingle();

        if (!bools.ToBoolean(5))
            zRot = buffer.ReadSingle();

        var state = new StateSnapshot
        {
            Position = new Vector3(xPos, yPos, zPos),
            Rotation = new Quat(new Vector3(xRot, yRot, zRot)),
        };

        while (stateInterpolations.Count > 2)
            stateInterpolations.Dequeue();

        stateInterpolations.Enqueue(state);
    }

    public virtual void PackSpawnState(BytesBuffer buffer)
    {
        buffer.Write(GlobalTranslation);
    }

    public virtual void OnRemoteSpawn(BytesBuffer buffer, GameProperties currentGame)
    {
        Translation = buffer.ReadVector3();
        Mode = ModeEnum.Static;
    }

    public virtual void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    protected void ClearInterpolations()
    {
        fromState = null;
        stateInterpolations.Clear();
    }

    private void InterpolateStates(float delta)
    {
        if (!NetworkManager.Instance.IsClient)
            return;

        lerpTimer += delta;

        var sendInterval = 1f / NetworkManager.Instance.ServerSettings.GetVar<int>("SendRate");

        if (lerpTimer > sendInterval)
        {
            lerpTimer = 0;

            if (stateInterpolations.Count > 1)
                fromState = stateInterpolations.Dequeue();
        }

        if (stateInterpolations.Count <= 0 || !fromState.HasValue)
            return;

        var toState = stateInterpolations.Peek();

        var weight = lerpTimer / sendInterval;

        var position = fromState.Value.Position.LinearInterpolate(toState.Position, weight);
        var rotation = fromState.Value.Rotation.Slerp(toState.Rotation, weight);

        GlobalTransform = new Transform(rotation, position);
    }

    public struct StateSnapshot
    {
        public Vector3 Position { get; set; }
        public Quat Rotation { get; set; }
    }
}
