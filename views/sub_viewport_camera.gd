extends Camera2D

@export var min_zoom: float = 0.15
@export var max_zoom: float = 4.0
@export var zoom_step: float = 1.15

var is_panning: bool = false


func _ready() -> void:
	make_current()


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_MIDDLE or event.button_index == MOUSE_BUTTON_RIGHT:
			is_panning = event.pressed
			get_viewport().set_input_as_handled()

		elif event.pressed:
			if event.button_index == MOUSE_BUTTON_WHEEL_UP:
				_zoom_at_point(zoom_step, event.position)
				get_viewport().set_input_as_handled()
			elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_zoom_at_point(1.0 / zoom_step, event.position)
				get_viewport().set_input_as_handled()

	elif event is InputEventMouseMotion and is_panning:
		# Safety check: if the user released the mouse button outside the SubViewport, stop panning
		if not Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE) and not Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
			is_panning = false
			return

		position -= event.relative / zoom.x
		get_viewport().set_input_as_handled()

	elif event is InputEventPanGesture:
		position += (event.delta * 15.0) / zoom.x
		get_viewport().set_input_as_handled()

	elif event is InputEventMagnifyGesture:
		var mouse_pos := get_viewport().get_mouse_position()
		_zoom_at_point(event.factor, mouse_pos)
		get_viewport().set_input_as_handled()


func _zoom_at_point(factor: float, screen_pos: Vector2) -> void:
	var new_zoom := clampf(zoom.x * factor, min_zoom, max_zoom)
	if is_equal_approx(zoom.x, new_zoom):
		return
	var world_before := screen_to_world(screen_pos)  # Get world coordinate under the mouse before changing zoom
	zoom = Vector2(new_zoom, new_zoom)  # Apply new zoom
	# Get world coordinate under the mouse AFTER changing zoom, and shift camera to keep it pinned
	var world_after := screen_to_world(screen_pos)
	position += (world_before - world_after)


## Converts a viewport pixel coordinate to a 2D graph world coordinate
func screen_to_world(screen_pos: Vector2) -> Vector2:
	var view_center := get_viewport_rect().size * 0.5
	return (screen_pos - view_center) / zoom.x + position


## Converts a 2D graph world coordinate to a viewport pixel coordinate
func world_to_screen(world_pos: Vector2) -> Vector2:
	var view_center := get_viewport_rect().size * 0.5
	return (world_pos - position) * zoom.x + view_center
