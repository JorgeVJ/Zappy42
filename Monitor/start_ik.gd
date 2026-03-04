@tool

extends SkeletonIK3D

func _ready():
	start()
	use_magnet = true;
	magnet.y = 0.5
