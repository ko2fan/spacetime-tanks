extends Control

@onready var create_button = $Lobby/CreateArenaButton
@onready var join_button = $Lobby/JoinArenaButton

@onready var arena_prefab = preload("res://Scenes/arena.tscn")

func _ready():
	hide();

func _on_create_arena_button_pressed():
	start_game()

func _on_join_arena_button_pressed():
	start_game()

func start_game():
	get_node("/root/Game/UI").hide();
	var arena = arena_prefab.instantiate()
	get_node("/root/Game").add_child(arena)
