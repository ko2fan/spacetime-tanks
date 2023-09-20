using Godot;
using System;
using SpacetimeDB.Types;

public partial class Bullet : Area2D
{
    private ulong entityId;
    
    public override void _Ready()
    {
        base._Ready();
        
        MobileLocationComponent.OnUpdate += MobileLocationComponent_OnUpdate;
        BulletComponent.OnDelete += BulletComponent_OnDelete;
    }
    
    public void SetEntityId(ulong entity)
    {
        entityId = entity;
    }
    
    void MobileLocationComponent_OnUpdate(MobileLocationComponent oldValue, MobileLocationComponent newValue,
        ReducerEvent reducerEvent)
    {
        if(newValue.EntityId == entityId)
        {
            // update the Tank position and direction
            StdbVector2 bulletPosition = newValue.Location;
            StdbVector2 bulletDirection = newValue.Direction;

            Position = new Vector2(bulletPosition.X, bulletPosition.Z);
            Vector2 direction = new Vector2(bulletDirection.X, bulletDirection.Z);
            LookAt(Position + direction);
        }
    }

    void BulletComponent_OnDelete(BulletComponent bulletComponent, ReducerEvent reducerEvent)
    {
        if (bulletComponent.EntityId == entityId)
        {
            MobileLocationComponent.OnUpdate -= MobileLocationComponent_OnUpdate;
            BulletComponent.OnDelete -= BulletComponent_OnDelete;
            QueueFree();
        }
    }
}
