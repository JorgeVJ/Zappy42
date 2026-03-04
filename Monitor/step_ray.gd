extends RayCast3D

@export var StepTarget: Node3D

func _ready():
	enabled = true
	print("XXXXXXXXXX Ready FrontLeftRay XXXXXXXXXXXXXXXXXX")

func _physics_process(_delta: float) -> void:
	if is_colliding():
		var hit_point = get_collision_point()
		print("Hit point")
		print(hit_point)
		if (hit_point):
			StepTarget.global_position = hit_point
