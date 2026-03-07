using Godot;
using System;

public partial class RayCamera : Camera3D
{
	//public Vector2I? GetTileUnderMouse()
	//{
	//    var camera = GetViewport().GetCamera3D();
	//    var mousePos = GetViewport().GetMousePosition();

	//    Vector3 origin = camera.ProjectRayOrigin(mousePos);
	//    Vector3 dir = camera.ProjectRayNormal(mousePos);

	//    var space = GetWorld3D().DirectSpaceState;

	//    var query = PhysicsRayQueryParameters3D.Create(
	//        origin,
	//        origin + dir * 1000
	//    );

	//    var result = space.IntersectRay(query);

	//    if (result.Count == 0)
	//        return null;

	//    Vector3 pos = (Vector3)result["position"];

	//    return terrain.GetTileFromPosition(pos);
	//}
}
