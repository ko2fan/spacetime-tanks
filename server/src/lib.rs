use std::ops::{Add, Mul};
use spacetimedb::{spacetimedb, Identity, SpacetimeType, Timestamp, ReducerContext};
use log;

#[spacetimedb(table)]
pub struct SpawnableEntityComponent {
    // All entities that can be spawned in the world will have this component.
    // This allows us to find all objects in the world by iterating through
    // this table. It also ensures that all world objects have a unique
    // entity_id.

    #[primarykey]
    #[autoinc]
    pub entity_id: u64,
}

#[derive(Clone)]
#[spacetimedb(table)]
pub struct PlayerComponent {
    // All players have this component and it associates the spawnable entity
    // with the user's identity. It also stores their username.

    #[primarykey]
    pub entity_id: u64,
    #[unique]
    pub owner_id: Identity,

    // username is provided to the create_player reducer
    pub username: String,
    // this value is updated when the user logs in and out
    pub logged_in: bool,
}

#[derive(Clone)]
#[spacetimedb(table)]
pub struct BulletComponent {
    // Spawned by player when they shoot

    #[primarykey]
    pub entity_id: u64,

    pub lifetime: f32,
}

#[derive(SpacetimeType, Clone)]
pub struct StdbVector2 {
    // A spacetime type which can be used in tables and reducers to represent
    // a 2d position.
    pub x: f32,
    pub z: f32,
}

impl Mul<f32> for StdbVector2 {
    type Output = StdbVector2;

    fn mul(self, rhs: f32) -> Self::Output {
        let x = self.x * rhs;
        let z = self.z * rhs;
        Self { x, z }
    }
}
impl Add for StdbVector2 {
    type Output = StdbVector2;

    fn add(self, rhs: Self) -> Self::Output {
        let x = self.x + rhs.x;
        let z = self.z + rhs.z;
        Self { x, z }
    }
}

impl StdbVector2 {
    // this allows us to use StdbVector2::ZERO in reducers
    pub const ZERO: StdbVector2 = StdbVector2 { x: 0.0, z: 0.0 };
}

#[spacetimedb(table)]
#[derive(Clone)]
pub struct MobileLocationComponent {
    // This component will be created for all world objects that can move
    // smoothly throughout the world. It keeps track of the position the last
    // time the component was updated and the direction the mobile object is
    // currently moving.

    #[primarykey]
    pub entity_id: u64,

    // The last known location of this entity
    pub location: StdbVector2,
    // Movement direction, {0,0} if not moving at all.
    pub direction: StdbVector2,
    // Timestamp when movement started. Timestamp::UNIX_EPOCH if not moving.
    pub move_start_timestamp: Timestamp,
}

#[spacetimedb(reducer)]
pub fn create_player(ctx: ReducerContext, username: String) -> Result<(), String> {
    // This reducer is called when the user logs in for the first time and
    // enters a username

    let owner_id = ctx.sender;
    // We check to see if there is already a PlayerComponent with this identity.
    // this should never happen because the client only calls it if no player
    // is found.
    if PlayerComponent::filter_by_owner_id(&owner_id).is_some() {
        log::info!("Player already exists");
        return Err("Player already exists".to_string());
    }

    // Next we create the SpawnableEntityComponent. The entity_id for this
    // component automatically increments and we get it back from the result
    // of the insert call and use it for all components.

    let entity_id = SpawnableEntityComponent::insert(SpawnableEntityComponent { entity_id: 0 })
        .expect("Failed to create player spawnable entity component.")
        .entity_id;
    // The PlayerComponent uses the same entity_id and stores the identity of
    // the owner, username, and whether or not they are logged in.
    PlayerComponent::insert(PlayerComponent {
        entity_id,
        owner_id,
        username: username.clone(),
        logged_in: true,
    })
        .expect("Failed to insert player component.");
    // The MobileLocationComponent is used to calculate the current position
    // of an entity that can move smoothly in the world. We are using 2d
    // positions and the client will use the terrain height for the y value.
    MobileLocationComponent::insert(MobileLocationComponent {
        entity_id,
        location: StdbVector2::ZERO,
        direction: StdbVector2::ZERO,
        move_start_timestamp: Timestamp::UNIX_EPOCH,
    })
        .expect("Failed to insert player mobile entity component.");

    log::info!("Player created: {}({})", username, entity_id);

    Ok(())
}

#[spacetimedb(reducer)]
pub fn shoot_bullet(_ctx: ReducerContext, location: StdbVector2, direction: StdbVector2) -> Result<(), String> {
    // This reducer is called when the player shoots a bullet

    let lifetime = 32.0;
    // Next we create the SpawnableEntityComponent. The entity_id for this
    // component automatically increments and we get it back from the result
    // of the insert call and use it for all components.

    let entity_id = SpawnableEntityComponent::insert(SpawnableEntityComponent { entity_id: 0 })
        .expect("Failed to create player spawnable entity component.")
        .entity_id;
    // The BulletComponent uses the same entity_id and stores the current lifetime
    BulletComponent::insert(BulletComponent { entity_id, lifetime, })
        .expect("Failed to insert bullet component.");

    // The MobileLocationComponent is used to calculate the current position
    // of an entity that can move smoothly in the world. We are using 2d
    // positions
    MobileLocationComponent::insert(MobileLocationComponent {
        entity_id,
        location,
        direction,
        move_start_timestamp: Timestamp::UNIX_EPOCH,
    })
        .expect("Failed to insert bullet mobile entity component.");

    log::info!("Bullet created: {}", entity_id);

    Ok(())
}

#[spacetimedb(init)]
pub fn init() {
    // Called when the module is initially published
    spacetimedb::schedule!("60ms", move_bullets(_, Timestamp::now()));
}

#[spacetimedb(connect)]
pub fn identity_connected(ctx: ReducerContext) {
    // called when the client connects, we update the logged_in state to true
    update_player_login_state(ctx, true);
}


#[spacetimedb(disconnect)]
pub fn identity_disconnected(ctx: ReducerContext) {
    // Called when the client disconnects, we update the logged_in state to false
    update_player_login_state(ctx, false);
}


pub fn update_player_login_state(ctx: ReducerContext, logged_in: bool) {
    // This helper function gets the PlayerComponent, sets the logged
    // in variable and updates the SpacetimeDB table row.
    if let Some(player) = PlayerComponent::filter_by_owner_id(&ctx.sender) {
        let entity_id = player.entity_id;
        // We clone the PlayerComponent so we can edit it and pass it back.
        let mut player = player.clone();
        player.logged_in = logged_in;
        PlayerComponent::update_by_entity_id(&entity_id, player);
    }
}

#[spacetimedb(reducer)]
pub fn move_player(
    ctx: ReducerContext,
    start: StdbVector2,
    direction: StdbVector2,
) -> Result<(), String> {
    // Update the MobileLocationComponent with the current movement
    // values. The client will call this regularly as the direction of movement
    // changes. A fully developed game should validate these moves on the server
    // before committing them, but that is beyond the scope of this tutorial.

    let owner_id = ctx.sender;
    // First, look up the player using the sender identity, then use that
    // entity_id to retrieve and update the MobileLocationComponent
    if let Some(player) = PlayerComponent::filter_by_owner_id(&owner_id) {
        if let Some(mut mobile) = MobileLocationComponent::filter_by_entity_id(&player.entity_id) {
            mobile.location = start;
            mobile.direction = direction;
            mobile.move_start_timestamp = ctx.timestamp;
            MobileLocationComponent::update_by_entity_id(&player.entity_id, mobile);


            return Ok(());
        }
    }

    // If we can not find the PlayerComponent for this user something went wrong.
    // This should never happen.
    return Err("Player not found".to_string());
}

#[spacetimedb(reducer, repeat = 60ms)]
pub fn move_bullets(_ctx: ReducerContext, _prev_time: Timestamp) -> Result<(), String> {
    // Update the MobileLocationComponent with the current movement
    // values. The server will repeatedly call this every 60ms

    // First, look up the player using the sender identity, then use that
    // entity_id to retrieve and update the MobileLocationComponent
    let bullet_speed = 50.0;
    for bullet in BulletComponent::iter() {
        if let Some(mut mobile) = MobileLocationComponent::filter_by_entity_id(&bullet.entity_id) {
            let old_mobile = mobile.clone();
            let new_position = old_mobile.location + old_mobile.direction * bullet_speed;
            mobile.location = new_position;
            MobileLocationComponent::update_by_entity_id(&bullet.entity_id, mobile);
        }

        let mut bullet = bullet.clone();
        let entity_id = bullet.entity_id;
        bullet.lifetime = bullet.lifetime - 1.0;
        if bullet.lifetime <= 0.0 {
            BulletComponent::delete_by_entity_id(&entity_id);
        } else {
            BulletComponent::update_by_entity_id(&entity_id, bullet);
        }
    }

    return Ok(());
}

#[spacetimedb(reducer)]
pub fn stop_player(ctx: ReducerContext, location: StdbVector2) -> Result<(), String> {
    // Update the MobileLocationComponent when a player comes to a stop. We set
    // the location to the current location and the direction to {0,0}
    let owner_id = ctx.sender;
    if let Some(player) = PlayerComponent::filter_by_owner_id(&owner_id) {
        if let Some(mut mobile) = MobileLocationComponent::filter_by_entity_id(&player.entity_id) {
            mobile.location = location;
            mobile.direction = StdbVector2::ZERO;
            mobile.move_start_timestamp = Timestamp::UNIX_EPOCH;
            MobileLocationComponent::update_by_entity_id(&player.entity_id, mobile);


            return Ok(());
        }
    }

    return Err("Player not found".to_string());
}