extends Marker3D

@export var StepTarget: Node3D
@export var StepDistance: float = 0.3
@export var StepHeight: float = 0.1

var is_stepping := false

func _ready():
	print("XXXXXXXXXX Ready FrontLeftMarker XXXXXXXXXXXXXXXXXX")

func _process(_delta: float) -> void:
	if (!is_stepping && abs(global_position.distance_to(StepTarget.global_position)) > StepDistance):
		Step()


func Step():
	print("XXXXXXXXXX Taking Step XXXXXXXXXXXXXXXXXX")
	var target = StepTarget.global_position
	var half_way = (global_position + target) * 0.5
	is_stepping = true
	
	var t = get_tree().create_tween()
	t.tween_property(self, "global_position", half_way + owner.basis.y * StepHeight, 0.2)
	t.tween_property(self, "global_position", target, 0.2)
	t.tween_callback(func(): is_stepping = false)
