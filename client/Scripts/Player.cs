using Godot;
using System;

public partial class Player : Node
{
    private ulong _entityId;
    private bool _localPlayer = false;
    public void SetEntityId(ulong entityId)
    {
        _entityId = entityId;
    }
    
    public ulong GetEntityId()
    {
        return _entityId;
    }

    public void SetLocal()
    {
        _localPlayer = true;
    }

    public bool GetLocal()
    {
        return _localPlayer;
    }
}
